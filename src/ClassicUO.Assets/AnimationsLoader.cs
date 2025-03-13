// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public unsafe sealed class AnimationsLoader : UOFileLoader
    {
        public const int MAX_ACTIONS = 80; // gargoyle is like 78
        public const int MAX_DIRECTIONS = 5;


        [ThreadStatic]
        private static FrameInfo[] _frames;

        private readonly UOFileMul[] _files = new UOFileMul[10];
        private readonly UOFileUop[] _filesUop = new UOFileUop[10];

        private readonly Dictionary<ushort, Dictionary<ushort, EquipConvData>> _equipConv = new Dictionary<ushort, Dictionary<ushort, EquipConvData>>();
        private readonly Dictionary<int, MobTypeInfo> _mobTypes = new Dictionary<int, MobTypeInfo>();
        private readonly Dictionary<int, BodyInfo> _bodyInfos = new Dictionary<int, BodyInfo>();
        private readonly Dictionary<int, BodyInfo> _corpseInfos = new Dictionary<int, BodyInfo>();
        private readonly Dictionary<int, BodyConvInfo> _bodyConvInfos = new Dictionary<int, BodyConvInfo>();
        private readonly Dictionary<int, UopInfo> _uopInfos = new Dictionary<int, UopInfo>();


        public AnimationsLoader(UOFileManager fileManager) : base(fileManager)
        {

        }

        public IReadOnlyDictionary<ushort, Dictionary<ushort, EquipConvData>> EquipConversions => _equipConv;

        public List<(ushort, byte)>[] GroupReplaces { get; } =
            new List<(ushort, byte)>[2]
            {
                new List<(ushort, byte)>(),
                new List<(ushort, byte)>()
            };


        public override void Load()
        {
            for (int i = 0; i < _files.Length; i++)
            {
                var pathmul = FileManager.GetUOFilePath("anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".mul");
                var pathidx = FileManager.GetUOFilePath("anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".idx");

                if (File.Exists(pathmul) && File.Exists(pathidx))
                {
                    _files[i] = new UOFileMul(pathmul, pathidx);
                }
            }

            if (FileManager.IsUOPInstallation)
            {
                var loaduop = false;

                for (var i = 0; i < _filesUop.Length; ++i)
                {
                    var pathuop = FileManager.GetUOFilePath($"AnimationFrame{i + 1}.uop");

                    if (File.Exists(pathuop))
                    {
                        _filesUop[i] = new UOFileUop(pathuop, "build/animationlegacyframe/{0:D6}/{0:D2}.bin");
                        _filesUop[i].FillEntries();
                        loaduop = true;
                    }
                }

                if (loaduop)
                {
                    LoadUop();
                }
            }

            if (FileManager.Version >= ClientVersion.CV_500A)
            {
                string path = FileManager.GetUOFilePath("mobtypes.txt");

                if (File.Exists(path))
                {
                    var typeNames = new string[5]
                    {
                        "monster",
                        "sea_monster",
                        "animal",
                        "human",
                        "equipment"
                    };

                    using (var reader = new StreamReader(File.OpenRead(path)))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();

                            if (line.Length == 0 || line[0] == '#' || !char.IsNumber(line[0]))
                            {
                                continue;
                            }

                            string[] parts = line.Split(
                                new[] { '\t', ' ' },
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length < 3)
                            {
                                continue;
                            }

                            int id = int.Parse(parts[0]);
                            string testType = parts[1].ToLower();
                            int commentIdx = parts[2].IndexOf('#');

                            if (commentIdx > 0)
                            {
                                parts[2] = parts[2].Substring(0, commentIdx - 1);
                            }
                            else if (commentIdx == 0)
                            {
                                continue;
                            }

                            uint number = uint.Parse(parts[2], NumberStyles.HexNumber);

                            for (int i = 0; i < 5; i++)
                            {
                                if (
                                    testType.Equals(
                                        typeNames[i],
                                        StringComparison.InvariantCultureIgnoreCase
                                    )
                                )
                                {
                                    _mobTypes[id] = new MobTypeInfo()
                                    {
                                        Type = (AnimationGroupsType)i,
                                        Flags = (AnimationFlags)(0x80000000 | number)
                                    };

                                    break;
                                }
                            }
                        }
                    }
                }
            }

            string file = FileManager.GetUOFilePath("Anim1.def");

            if (File.Exists(file))
            {
                using (DefReader defReader = new DefReader(file))
                {
                    while (defReader.Next())
                    {
                        ushort group = (ushort)defReader.ReadInt();

                        if (group == 0xFFFF)
                        {
                            continue;
                        }

                        int replace = defReader.ReadGroupInt();

                        GroupReplaces[0].Add((group, (byte)replace));
                    }
                }
            }

            file = FileManager.GetUOFilePath("Anim2.def");

            if (File.Exists(file))
            {
                using (DefReader defReader = new DefReader(file))
                {
                    while (defReader.Next())
                    {
                        ushort group = (ushort)defReader.ReadInt();

                        if (group == 0xFFFF)
                        {
                            continue;
                        }

                        int replace = defReader.ReadGroupInt();

                        GroupReplaces[1].Add((group, (byte)replace));
                    }
                }
            }

            ProcessEquipConvDef();
            ProcessBodyDef();
            ProcessCorpseDef();
        }

        public bool ReplaceBody(ref ushort body, ref ushort hue)
        {
            if (_bodyInfos.TryGetValue(body, out var bodyInfo))
            {
                body = bodyInfo.Graphic;
                hue = bodyInfo.Hue;

                return true;
            }

            return false;
        }

        public bool ReplaceCorpse(ref ushort body, ref ushort hue)
        {
            if (_corpseInfos.TryGetValue(body, out var bodyInfo))
            {
                body = bodyInfo.Graphic;
                hue = bodyInfo.Hue;

                return true;
            }

            return false;
        }

        public bool ReplaceUopGroup(ushort body, ref byte group)
        {
            if (_uopInfos.TryGetValue(body, out var uopInfo))
            {
                group = (byte)uopInfo.ReplacedAnimations[group];

                return true;
            }

            return false;
        }

        public ReadOnlySpan<AnimationDirection> GetIndices
        (
            ClientVersion clientVersion,
            ushort body,
            ref ushort hue,
            ref AnimationFlags flags,
            out int fileIndex,
            out AnimationGroupsType animType
        )
        {
            fileIndex = 0;
            animType = AnimationGroupsType.Unknown;

            if (!_mobTypes.TryGetValue(body, out var mobInfo))
            {
                mobInfo.Flags = AnimationFlags.None;
                mobInfo.Type = AnimationGroupsType.Unknown;
            }

            flags = mobInfo.Flags;

            if ((mobInfo.Flags & AnimationFlags.UseUopAnimation) != 0)
            {
                if (animType == AnimationGroupsType.Unknown)
                    animType = mobInfo.Type != AnimationGroupsType.Unknown ? mobInfo.Type : CalculateTypeByGraphic(body);

                var replaceFound = _uopInfos.TryGetValue(body, out var uopInfo);
                var animIndices = Array.Empty<AnimationDirection>();

                for (int actioIdx = 0; actioIdx < MAX_ACTIONS; ++actioIdx)
                {
                    var action = replaceFound ? uopInfo.ReplacedAnimations[actioIdx] : actioIdx;
                    var hashString = $"build/animationlegacyframe/{body:D6}/{action:D2}.bin";
                    var hash = UOFileUop.CreateHash(hashString);

                    for (int index = 0; index < _filesUop.Length; ++index)
                    {
                        if (_filesUop[index] != null && _filesUop[index].TryGetUOPData(hash, out var data))
                        {
                            if (animIndices.Length == 0)
                                animIndices = new AnimationDirection[MAX_ACTIONS];

                            fileIndex = index;

                            ref var animIndex = ref animIndices[actioIdx];
                            animIndex.Position = (uint)data.Offset;
                            animIndex.Size = (uint)data.Length;
                            animIndex.UncompressedSize = (uint)data.DecompressedLength;
                            animIndex.CompressionType = data.CompressionFlag;

                            break;
                        }
                    }
                }

                return animIndices;
            }

            if (_bodyConvInfos.TryGetValue(body, out var bodyConvInfo))
            {
                hue = bodyConvInfo.Hue;
                body = bodyConvInfo.Graphic;
                fileIndex = bodyConvInfo.FileIndex;

                if (clientVersion < ClientVersion.CV_500A)
                    animType = bodyConvInfo.AnimType;
            }

            if (animType == AnimationGroupsType.Unknown)
                animType = mobInfo.Type != AnimationGroupsType.Unknown ? mobInfo.Type : CalculateTypeByGraphic(body, fileIndex);

            var fileIdx = _files[fileIndex].IdxFile;
            var offsetAddress = CalculateOffset(body, animType, flags, out var actionCount);

            var offset = fileIdx.Position + offsetAddress;
            var end = fileIdx.Position + fileIdx.Length;

            if (offset >= end)
            {
                return ReadOnlySpan<AnimationDirection>.Empty;
            }

            if (offset + (actionCount * MAX_DIRECTIONS * sizeof(AnimIdxBlock)) > end)
            {
                return ReadOnlySpan<AnimationDirection>.Empty;
            }


            fileIdx.Seek(offsetAddress, SeekOrigin.Begin);

            var size = actionCount * MAX_DIRECTIONS;

            var indicesBuf = ArrayPool<AnimIdxBlock>.Shared.Rent(size);
            fileIdx.Read(MemoryMarshal.AsBytes(indicesBuf.AsSpan(0, size)));

            var directions = new AnimationDirection[size];
            for (var i = 0; i < directions.Length; ++i)
            {
                ref var dir = ref directions[i];
                ref var index = ref indicesBuf[i];
                dir.Position = index.Position;
                dir.Size = index.Size;
                dir.UncompressedSize = index.Unknown;
                dir.CompressionType = CompressionType.None;
            }

            ArrayPool<AnimIdxBlock>.Shared.Return(indicesBuf);
            return directions;
        }

        private long CalculateOffset(
            ushort graphic,
            AnimationGroupsType type,
            AnimationFlags flags,
            out int groupCount
        )
        {
            long result = 0;
            groupCount = 0;

            var group = AnimationGroups.None;

            switch (type)
            {
                case AnimationGroupsType.Monster:

                    if ((flags & AnimationFlags.CalculateOffsetByPeopleGroup) != 0)
                    {
                        group = AnimationGroups.People;
                    }
                    else if ((flags & AnimationFlags.CalculateOffsetByLowGroup) != 0)
                    {
                        group = AnimationGroups.Low;
                    }
                    else
                    {
                        group = AnimationGroups.High;
                    }

                    break;

                case AnimationGroupsType.SeaMonster:
                    result = CalculateHighGroupOffset(graphic);
                    groupCount = (int)LowAnimationGroup.AnimationCount;

                    break;

                case AnimationGroupsType.Animal:

                    if ((flags & AnimationFlags.CalculateOffsetLowGroupExtended) != 0)
                    {
                        if ((flags & AnimationFlags.CalculateOffsetByPeopleGroup) != 0)
                        {
                            group = AnimationGroups.People;
                        }
                        else if ((flags & AnimationFlags.CalculateOffsetByLowGroup) != 0)
                        {
                            group = AnimationGroups.Low;
                        }
                        else
                        {
                            group = AnimationGroups.High;
                        }
                    }
                    else
                    {
                        group = AnimationGroups.Low;
                    }

                    break;

                default:
                    group = AnimationGroups.People;

                    break;
            }

            switch (group)
            {
                case AnimationGroups.Low:
                    result = CalculateLowGroupOffset(graphic);
                    groupCount = (int)LowAnimationGroup.AnimationCount;

                    break;

                case AnimationGroups.High:
                    result = CalculateHighGroupOffset(graphic);
                    groupCount = (int)HighAnimationGroup.AnimationCount;

                    break;

                case AnimationGroups.People:
                    result = CalculatePeopleGroupOffset(graphic);
                    groupCount = (int)PeopleAnimationGroup.AnimationCount;

                    break;
            }

            return result;
        }

        private void ProcessEquipConvDef()
        {
            if (FileManager.Version < ClientVersion.CV_300)
            {
                return;
            }

            var file = FileManager.GetUOFilePath("Equipconv.def");

            if (File.Exists(file))
            {
                using (DefReader defReader = new DefReader(file, 5))
                {
                    while (defReader.Next())
                    {
                        ushort body = (ushort)defReader.ReadInt();
                        ushort graphic = (ushort)defReader.ReadInt();
                        ushort newGraphic = (ushort)defReader.ReadInt();
                        int gump = defReader.ReadInt();

                        if (gump > ushort.MaxValue)
                        {
                            continue;
                        }

                        if (gump == 0)
                        {
                            gump = graphic;
                        }
                        else if (gump == 0xFFFF || gump == -1)
                        {
                            gump = newGraphic;
                        }

                        ushort color = (ushort)defReader.ReadInt();

                        if (!_equipConv.TryGetValue(body, out var dict))
                        {
                            _equipConv[body] = (dict = new Dictionary<ushort, EquipConvData>());
                        }

                        dict[graphic] = new EquipConvData(newGraphic, (ushort)gump, color);
                    }
                }
            }
        }

        public void ProcessBodyConvDef(BodyConvFlags flags)
        {
            if (FileManager.Version < ClientVersion.CV_300)
            {
                return;
            }

            var file = FileManager.GetUOFilePath("Bodyconv.def");

            if (!File.Exists(file))
                return;

            using (var defReader = new DefReader(file))
            {
                while (defReader.Next())
                {
                    ushort index = (ushort)defReader.ReadInt();
                    for (int i = 1; i < defReader.PartsCount; i++)
                    {
                        int body = defReader.ReadInt();
                        if (body < 0)
                        {
                            continue;
                        }

                        // Ensure the client is allowed to use these new graphics
                        if (i == 1)
                        {
                            if (!flags.HasFlag(BodyConvFlags.Anim1))
                            {
                                continue;
                            }
                        }
                        else if (i == 2)
                        {
                            if (!flags.HasFlag(BodyConvFlags.Anim2))
                            {
                                continue;
                            }
                        }

                        // NOTE: for fileindex >= 3 the client automatically accepts body conversion.
                        //       Probably it ignores the flags
                        /*else if (i == 3)
                        {
                            if (flags.HasFlag(BodyConvFlags.Anim3))
                            {
                                continue;
                            }
                        }
                        else if (i == 4)
                        {
                            if (flags.HasFlag(BodyConvFlags.Anim4))
                            {
                                continue;
                            }
                        }
                        */

                        sbyte mountedHeightOffset = 0;
                        if (i == 1)
                        {
                            if (index == 0x00C0 || index == 793)
                            {
                                mountedHeightOffset = -9;
                            }
                        }
                        else if (i == 2)
                        {
                            if (index == 0x0579)
                            {
                                mountedHeightOffset = 9;
                            }
                        }
                        else if (i == 4)
                        {
                            mountedHeightOffset = -9;

                            if (index == 0x0115 || index == 0x00C0)
                            {
                                mountedHeightOffset = 0;
                            }
                            else if (index == 0x042D)
                            {
                                mountedHeightOffset = 3;
                            }
                        }

                        if (i >= _files.Length || _files[i] == null)
                        {
                            continue;
                        }

                        _bodyConvInfos[index] = new BodyConvInfo()
                        {
                            FileIndex = i,
                            Graphic = (ushort)body,
                            // TODO: fix for UOFileManager.Version < ClientVersion.CV_500A
                            AnimType = CalculateTypeByGraphic((ushort)body, i),
                            MountHeight = mountedHeightOffset
                        };
                    }
                }
            }
        }

        private void ProcessBodyDef()
        {
            if (FileManager.Version < ClientVersion.CV_300)
            {
                return;
            }

            var file = FileManager.GetUOFilePath("Body.def");

            if (!File.Exists(file))
                return;

            using (var defReader = new DefReader(file, 1))
            {
                while (defReader.Next())
                {
                    int index = defReader.ReadInt();

                    if (_bodyInfos.TryGetValue(index, out var info) && info.Graphic != 0)
                    {
                        continue;
                    }

                    int[] group = defReader.ReadGroup();

                    if (group == null)
                    {
                        continue;
                    }

                    int color = defReader.ReadInt();

                    //Yes, this is actually how this is supposed to work.
                    var checkIndex = group.Length >= 3 ? group[2] : group[0];

                    _bodyInfos[index] = new BodyInfo()
                    {
                        Graphic = (ushort)checkIndex,
                        Hue = (ushort)color
                    };
                }
            }
        }

        private void ProcessCorpseDef()
        {
            if (FileManager.Version < ClientVersion.CV_300)
            {
                return;
            }

            var file = FileManager.GetUOFilePath("Corpse.def");

            if (!File.Exists(file))
                return;

            using (var defReader = new DefReader(file, 1))
            {
                while (defReader.Next())
                {
                    int index = defReader.ReadInt();

                    if (_corpseInfos.TryGetValue(index, out var b) && b.Graphic != 0)
                    {
                        continue;
                    }

                    int[] group = defReader.ReadGroup();

                    if (group == null)
                    {
                        continue;
                    }

                    int color = defReader.ReadInt();
                    int checkIndex = group.Length >= 3 ? group[2] : group[0];

                    _corpseInfos[index] = new BodyInfo()
                    {
                        Graphic = (ushort)checkIndex,
                        Hue = (ushort)color
                    };
                }
            }
        }

        private void LoadUop()
        {
            if (FileManager.Version <= ClientVersion.CV_60144)
            {
                return;
            }

            var animationSequencePath = FileManager.GetUOFilePath("AnimationSequence.uop");

            if (!File.Exists(animationSequencePath))
            {
                Log.Warn("AnimationSequence.uop not found");

                return;
            }

            // ==========================
            // credit: @tristran
            // ==========================
            // u32 animid
            // 12 times: [
            //   u32 unk0 //often zero
            // ]
            // //--------------
            // u32 replace
            // replace times: [
            //   u32 oldgroup
            //   u32 framecount
            //   u32 newgroup
            //   //if newgroup not is -1 then this animation group is replaced by that group
            //   u32 flags1 //unsure what these mean often 0x41100000
            //   16 times: [ //maybe something to do with mounts but...
            //     u8 unk1 //if newgroup ==-1 usually -128 else usually 0
            //   ]
            //   8 times: [
            //     u32 unk2 //often 0 animation 826 has something different...
            //   ]
            //   u32 num1     //rarely present but human (400) has them for oldgroup 0,1,2,3,23,24,35 (stand/walk/run)
            //   num1 times: [
            //     u32 w0
            //     u32 w1
            //     u32 w2
            //     u32 w3
            //     u32 w4
            //     u32 w5
            //     u16 s6
            //     u32 w7
            //     u16 s8
            //   ]
            //   u32 num2
            //   num2 times: [
            //     u32 unk3
            //   ]
            // ]
            // //-----------
            // u32 xtra
            // xtra times: [
            //   u8 mob_mode //identifies the "mode" this defintion belongs to combat(0)/id(1)/ride (2) /fly(3)/fly combat?(4)/fly idle(5) /sit(6)
            //   s8 b2       //fallback mode?
            //   u32 def_action // default action (fallback if action not in following structure)
            //   u32 num1
            //   num1 times: [ //transition to other mode? (see gargoyle (666))
            //     u8 b6     //mode
            //     u32 n5    //anim/group
            //   ]
            //   u32 num2    //usually 3 (stand, walk, run)
            //   num2 times: [
            //     u8 action
            //     u32 anim1h //one handed?
            //     u32 anim2h //two handed?
            //   ]
            //   u32 num3    // NewCharacterAnimation
            //   num3 times: [
            //     s8 type      //actual action fight etc
            //     s8 action    //sub action
            //     u32 num4     //random select one of the list
            //     num4 times: [
            //       u32 anim   //group
            //     ]
            //   ]
            // ]

            /*
            based on the current "mode" the mobile is in (e.g. IsFlying check) select the right set of definitions from the xtra array
            then consult the num2 based list for stand/walk/run
            and the num3 based list for NewCharacterAnimation packets
            /
            / flags
            41100000
            41400000 usually group 22,24 (walk run?)
            40C00000 often group 31
            42860000 anim 692   Animated weapon
            41F80000 anim 692
            41300000 anim 1246,1247 , group 0  (jack o lantern)
            */

            var animSeq = new UOFileUop(
                animationSequencePath,
                "build/animationsequence/{0:D8}.bin"
            );

            animSeq.FillEntries();

            var buf = new byte[1024];
            var dbuf = new byte[1024];

            foreach (var entry in animSeq.Entries)
            {
                if (entry.Length == 0)
                    continue;

                animSeq.Seek(entry.Offset, SeekOrigin.Begin);

                if (buf.Length < entry.Length)
                    buf = new byte[entry.Length];

                animSeq.Read(buf.AsSpan(0, entry.Length));
                var reader = new StackDataReader(buf);
                if (entry.CompressionFlag >= CompressionType.Zlib)
                {
                    if (dbuf.Length < entry.DecompressedLength)
                        dbuf = new byte[entry.DecompressedLength];

                    var ok = ZLib.Decompress(buf.AsSpan(0, entry.Length), dbuf.AsSpan(0, entry.DecompressedLength));
                    if (ok != ZLib.ZLibError.Ok)
                        continue;

                    reader = new StackDataReader(dbuf.AsSpan(0, entry.DecompressedLength));
                }

                if (reader.Remaining <= 0)
                {
                    continue;
                }

                uint animID = reader.ReadUInt32LE();
                reader.Skip(48);
                int replaces = reader.ReadInt32LE();

                var uopInfo = new UopInfo();
                var j = 0;
                foreach (ref var idx in uopInfo.ReplacedAnimations)
                    idx = j++;

                if (replaces != 48 && replaces != 68)
                {
                    for (int k = 0; k < replaces; k++)
                    {
                        int oldGroup = reader.ReadInt32LE();
                        uint frameCount = reader.ReadUInt32LE();
                        int newGroup = reader.ReadInt32LE();

                        if (frameCount == 0)
                        {
                            uopInfo.ReplacedAnimations[oldGroup] = newGroup;
                        }

                        reader.Skip(60);
                    }
                }

                _uopInfos[(int)animID] = uopInfo;
            }

            animSeq.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint CalculatePeopleGroupOffset(ushort graphic)
        {
            return (uint)(((graphic - 400) * 175 + 35000) * sizeof(AnimIdxBlock));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint CalculateHighGroupOffset(ushort graphic)
        {
            return (uint)(graphic * 110 * sizeof(AnimIdxBlock));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint CalculateLowGroupOffset(ushort graphic)
        {
            return (uint)(((graphic - 200) * 65 + 22000) * sizeof(AnimIdxBlock));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AnimationGroupsType CalculateTypeByGraphic(ushort graphic, int fileIndex = 0)
        {
            if (fileIndex == 1) // anim2
            {
                return graphic < 200 ? AnimationGroupsType.Monster : AnimationGroupsType.Animal;
            }

            if (fileIndex == 2) // anim3
            {
                return graphic < 300
                    ? AnimationGroupsType.Animal
                    : graphic < 400
                        ? AnimationGroupsType.Monster
                        : AnimationGroupsType.Human;
            }

            return graphic < 200
                ? AnimationGroupsType.Monster
                : graphic < 400
                    ? AnimationGroupsType.Animal
                    : AnimationGroupsType.Human;
        }

        public override void ClearResources() { }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetSittingAnimDirection(ref byte dir, ref bool mirror, ref int x, ref int y)
        {
            switch (dir)
            {
                case 0:
                    mirror = true;
                    dir = 3;

                    break;

                case 2:
                    mirror = true;
                    dir = 1;

                    break;

                case 4:
                    mirror = false;
                    dir = 1;

                    break;

                case 6:
                    mirror = false;
                    dir = 3;

                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixSittingDirection(
            ref byte direction,
            ref bool mirror,
            ref int x,
            ref int y,
            ref SittingInfoData data
        )
        {
            switch (direction)
            {
                case 7:
                case 0:
                    {
                        if (data.Direction1 == -1)
                        {
                            if (direction == 7)
                            {
                                direction = (byte)data.Direction4;
                            }
                            else
                            {
                                direction = (byte)data.Direction2;
                            }
                        }
                        else
                        {
                            direction = (byte)data.Direction1;
                        }

                        break;
                    }

                case 1:
                case 2:
                    {
                        if (data.Direction2 == -1)
                        {
                            if (direction == 1)
                            {
                                direction = (byte)data.Direction1;
                            }
                            else
                            {
                                direction = (byte)data.Direction3;
                            }
                        }
                        else
                        {
                            direction = (byte)data.Direction2;
                        }

                        break;
                    }

                case 3:
                case 4:
                    {
                        if (data.Direction3 == -1)
                        {
                            if (direction == 3)
                            {
                                direction = (byte)data.Direction2;
                            }
                            else
                            {
                                direction = (byte)data.Direction4;
                            }
                        }
                        else
                        {
                            direction = (byte)data.Direction3;
                        }

                        break;
                    }

                case 5:
                case 6:
                    {
                        if (data.Direction4 == -1)
                        {
                            if (direction == 5)
                            {
                                direction = (byte)data.Direction3;
                            }
                            else
                            {
                                direction = (byte)data.Direction1;
                            }
                        }
                        else
                        {
                            direction = (byte)data.Direction4;
                        }

                        break;
                    }
            }

            GetSittingAnimDirection(ref direction, ref mirror, ref x, ref y);

            const int SITTING_OFFSET_X = 8;

            int offsX = SITTING_OFFSET_X;

            if (mirror)
            {
                if (direction == 3)
                {
                    y += 25 + data.MirrorOffsetY;
                    x += offsX - 4;
                }
                else
                {
                    y += data.OffsetY + 9;
                }
            }
            else
            {
                if (direction == 3)
                {
                    y += 23 + data.MirrorOffsetY;
                    x -= 3;
                }
                else
                {
                    y += 10 + data.OffsetY;
                    x -= offsX + 1;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnimationGroups GetGroupIndex(ushort graphic, AnimationGroupsType animType)
        {
            switch (animType)
            {
                case AnimationGroupsType.Animal:
                    return AnimationGroups.Low;

                case AnimationGroupsType.Monster:
                case AnimationGroupsType.SeaMonster:
                    return AnimationGroups.High;

                case AnimationGroupsType.Human:
                case AnimationGroupsType.Equipment:
                    return AnimationGroups.People;
            }

            return AnimationGroups.High;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetDeathAction(
            ushort animID,
            AnimationFlags animFlags,
            AnimationGroupsType animType,
            bool second,
            bool isRunning = false
        )
        {
            //ConvertBodyIfNeeded(ref animID);

            if (animFlags.HasFlag(AnimationFlags.CalculateOffsetByLowGroup))
            {
                animType = AnimationGroupsType.Animal;
            }

            if (animFlags.HasFlag(AnimationFlags.CalculateOffsetLowGroupExtended))
            {
                animType = AnimationGroupsType.Monster;
            }

            switch (animType)
            {
                case AnimationGroupsType.Animal:

                    if (
                        (animFlags & AnimationFlags.Use2IfHittedWhileRunning) != 0
                        || (animFlags & AnimationFlags.CanFlying) != 0
                    )
                    {
                        return 2;
                    }

                    if ((animFlags & AnimationFlags.UseUopAnimation) != 0)
                    {
                        return (byte)(second ? 3 : 2);
                    }

                    return (byte)(
                        second ? LowAnimationGroup.Die2 : LowAnimationGroup.Die1
                    );

                case AnimationGroupsType.SeaMonster:
                    {
                        if (!isRunning)
                        {
                            return 8;
                        }

                        goto case AnimationGroupsType.Monster;
                    }

                case AnimationGroupsType.Monster:

                    if ((animFlags & AnimationFlags.UseUopAnimation) != 0)
                    {
                        return (byte)(second ? 3 : 2);
                    }

                    return (byte)(
                        second ? HighAnimationGroup.Die2 : HighAnimationGroup.Die1
                    );

                case AnimationGroupsType.Human:
                case AnimationGroupsType.Equipment:
                    return (byte)(
                        second ? PeopleAnimationGroup.Die2 : PeopleAnimationGroup.Die1
                    );
            }

            return 0;
        }

        public Span<FrameInfo> ReadUOPAnimationFrames(
            ushort animID,
            byte animGroup,
            byte direction,
            AnimationGroupsType type,
            int fileIndex,
            AnimationDirection index
        )
        {
            if (fileIndex < 0 || fileIndex >= _filesUop.Length)
            {
                return Span<FrameInfo>.Empty;
            }

            var file = _filesUop[fileIndex];

            if (index.Position == 0 && index.Size == 0)
            {
                return Span<FrameInfo>.Empty;
            }

            if (_frames == null)
            {
                _frames = new FrameInfo[22];
            }

            if (
                fileIndex == 0
                && index.Size == 0
                && index.UncompressedSize == 0
                && index.Position == 0
            )
            {
                Log.Warn("uop animData is null");

                return Span<FrameInfo>.Empty;
            }

            file.Seek(index.Position, SeekOrigin.Begin);
            var buf = new byte[index.Size];
            file.Read(buf);

            var reader = new StackDataReader(buf);

            if (index.CompressionType >= CompressionType.Zlib)
            {
                var dbuf = new byte[(int)index.UncompressedSize];
                var result = ZLib.Decompress(buf, dbuf);
                if (result != ZLib.ZLibError.Ok)
                {
                    Log.Error($"error reading uop animation. AnimID: {animID} | Group: {animGroup} | Dir: {direction} | FileIndex: {fileIndex}");

                    return Span<FrameInfo>.Empty;
                }

                if (index.CompressionType == CompressionType.ZlibBwt)
                {
                    dbuf = ClassicUO.Utility.BwtDecompress.Decompress(dbuf);
                }

                reader = new StackDataReader(dbuf);
            }

            reader.Skip(32);

            int fc = reader.ReadInt32LE();
            uint dataStart = reader.ReadUInt32LE();
            reader.Seek(dataStart);

            UOPFrameData[] sharedBuffer = ArrayPool<UOPFrameData>.Shared.Rent(fc);
            try
            {
                var frameData = sharedBuffer.AsSpan(0, fc);
                frameData.Clear();

                for (var i = 0; i < fc; ++i)
                {
                    var start = reader.Position;

                    var group = reader.ReadUInt16LE();
                    var frameId = reader.ReadUInt16LE();
                    reader.ReadUInt64LE();
                    var pixeloffset = reader.ReadUInt32LE();

                    ref var frame = ref frameData[i];
                    frame.Position = start;
                    frame.Group = group;
                    frame.FrameID = frameId;
                    frame.PixelOffset = pixeloffset;
                }

                var list = new List<UOPFrameData>();
                var lastFrameId = 1;
                for (var i = 0; i < fc; ++i)
                {
                    while (frameData[i].FrameID - lastFrameId > 1)
                    {
                        lastFrameId += 1;
                        list.Add(new()
                        {
                            Position = 0, // make sure we treat it as an empty frame
                            FrameID = lastFrameId
                        });
                    }

                    list.Add(frameData[i]);
                    lastFrameId = frameData[i].FrameID;
                }

                frameData = CollectionsMarshal.AsSpan(list);
                var maxFrameCount = frameData.Length;

                // Looks like the min amount of frames is 10 for equipment
                var realFrameCount = type == AnimationGroupsType.Equipment ?
                    Math.Max(10, (int)Math.Round(maxFrameCount / (float)MAX_DIRECTIONS))
                    :
                    (int)Math.Round(maxFrameCount / (float)MAX_DIRECTIONS);


                if (realFrameCount > _frames.Length)
                {
                    _frames = new FrameInfo[realFrameCount];
                }

                var framesSpan = _frames.AsSpan(0, realFrameCount);
                framesSpan.Clear();

                // var dirFrameStartIdx = realFrameCount * direction;

                foreach (ref readonly var frame in frameData)
                {
                    // validate the group only if the frame is valid
                    if (frame.Position > 0)
                    {
                        if (frame.Group != animGroup)
                        {
                            // we dont ignore here, might be the AnimationSequence.uop that changes the group
                            // continue;
                        }
                    }

                    var frameDirection = (frame.FrameID - 1) / realFrameCount;

                    if (frameDirection < direction)
                    {
                        // still not getting the right direction yet
                        continue;
                    }

                    if (frameDirection > direction)
                    {
                        // end of the direction
                        break;
                    }

                    var idx = (frame.FrameID - 1) % realFrameCount;
                    ref var frameInfo = ref framesSpan[idx];

                    // we need to zero-out the frame or we will see ghost animations coming from other animation queries
                    frameInfo.Num = idx;
                    frameInfo.CenterX = 0;
                    frameInfo.CenterY = 0;
                    frameInfo.Width = 0;
                    frameInfo.Height = 0;

                    // if it's a missing frame, we skip, but the animation gets tracked
                    if (frame.Position == 0)
                    {
                        continue;
                    }

                    reader.Seek(frame.Position + frame.PixelOffset);

                    var palette = MemoryMarshal.Cast<byte, ushort>(reader.Buffer.Slice(reader.Position, 512));
                    reader.Skip(512);

                    ReadSpriteData(ref reader, palette, ref frameInfo, true);
                }

                return framesSpan;
            }
            finally
            {
                ArrayPool<UOPFrameData>.Shared.Return(sharedBuffer);
            }
        }

        public Span<FrameInfo> ReadMULAnimationFrames(int fileIndex, AnimationDirection index)
        {
            if (fileIndex < 0 || fileIndex >= _files.Length)
            {
                return Span<FrameInfo>.Empty;
            }

            if (index.Position == 0 && index.Size == 0)
            {
                return Span<FrameInfo>.Empty;
            }

            if (index.Position == 0xFFFF_FFFF || index.Size == 0xFFFF_FFFF || index.Size <= 0)
            {
                return Span<FrameInfo>.Empty;
            }

            var file = _files[fileIndex];

            if (index.Position + index.Size > file.Length)
            {
                return Span<FrameInfo>.Empty;
            }

            // TODO: check if UOFileIndex works
            file.Seek(index.Position, SeekOrigin.Begin);
            var buf = new byte[index.Size];
            file.Read(buf);

            var reader = new StackDataReader(buf);
            var palette = MemoryMarshal.Cast<byte, ushort>(reader.Buffer.Slice(reader.Position, 512));
            reader.Skip(512);

            long dataStart = reader.Position;
            uint frameCount = reader.ReadUInt32LE();
            var frameOffset = new ReadOnlySpan<uint>((uint*)reader.PositionAddress, (int)frameCount);

            if (_frames == null || frameCount > _frames.Length)
            {
                _frames = new FrameInfo[frameCount];
            }

            var frames = _frames.AsSpan(0, (int)frameCount);

            for (int i = 0; i < frameCount; i++)
            {
                reader.Seek(dataStart + frameOffset[i]);

                frames[i].Num = i;
                ReadSpriteData(ref reader, palette, ref frames[i], false);
            }

            return frames;
        }

        private void ReadSpriteData(
            ref StackDataReader reader,
            ReadOnlySpan<ushort> palette,
            ref FrameInfo frame,
            bool alphaCheck
        )
        {
            frame.CenterX = reader.ReadInt16LE();
            frame.CenterY = reader.ReadInt16LE();
            frame.Width = reader.ReadInt16LE();
            frame.Height = reader.ReadInt16LE();

            if (frame.Width <= 0 || frame.Height <= 0)
            {
                return;
            }

            int bufferSize = frame.Width * frame.Height;

            if (frame.Pixels == null || frame.Pixels.Length < bufferSize)
            {
                frame.Pixels = new uint[bufferSize];
            }
            else
            {
                frame.Pixels.AsSpan().Slice(0, bufferSize).Fill(0);
            }

            Span<uint> data = frame.Pixels;

            uint header = reader.ReadUInt32LE();

            while (header != 0x7FFF7FFF && reader.Position < reader.Length)
            {
                ushort runLength = (ushort)(header & 0x0FFF);
                int x = (int)((header >> 22) & 0x03FF);

                if ((x & 0x0200) > 0)
                {
                    x |= unchecked((int)0xFFFFFE00);
                }

                int y = (int)((header >> 12) & 0x3FF);

                if ((y & 0x0200) > 0)
                {
                    y |= unchecked((int)0xFFFFFE00);
                }

                x += frame.CenterX;
                y += frame.CenterY + frame.Height;

                int block = y * frame.Width + x;

                for (int k = 0; k < runLength; ++k, ++block)
                {
                    ushort val = palette[reader.ReadUInt8()];

                    // FIXME: same of MUL ? Keep it as original for the moment
                    if (!alphaCheck || val != 0)
                    {
                        data[block] = HuesHelper.Color16To32(val) | 0xFF_00_00_00;
                    }
                    else
                    {
                        data[block] = 0;
                    }
                }

                header = reader.ReadUInt32LE();
            }
        }

        public struct FrameInfo
        {
            public int Num;
            public short CenterX;
            public short CenterY;
            public short Width;
            public short Height;
            public uint[] Pixels;
        }

        public struct SittingInfoData
        {
            public SittingInfoData(
                ushort graphic,
                sbyte d1,
                sbyte d2,
                sbyte d3,
                sbyte d4,
                sbyte offsetY,
                sbyte mirrorOffsetY,
                bool drawback
            )
            {
                Graphic = graphic;
                Direction1 = d1;
                Direction2 = d2;
                Direction3 = d3;
                Direction4 = d4;
                OffsetY = offsetY;
                MirrorOffsetY = mirrorOffsetY;
                DrawBack = drawback;
            }

            public readonly ushort Graphic;
            public readonly sbyte Direction1,
                Direction2,
                Direction3,
                Direction4;
            public readonly sbyte OffsetY,
                MirrorOffsetY;
            public readonly bool DrawBack;

            public static SittingInfoData Empty = new SittingInfoData();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AnimIdxBlock
        {
            public uint Position;
            public uint Size;
            public uint Unknown;
        }

        public struct AnimationDirection
        {
            public uint Position;
            public uint Size;
            public uint UncompressedSize;
            public CompressionType CompressionType;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct UOPAnimationHeader
        {
            public ushort Group;
            public ushort FrameID;

            public ushort Unk0;
            public ushort Unk1;
            public ushort Unk2;
            public ushort Unk3;

            public uint DataOffset;
        }

        [DebuggerDisplay("FramID: {FrameID}")]
        struct UOPFrameData
        {
            public long Position;
            public uint PixelOffset;
            public int FrameID, Group;
        }
    }

    public enum AnimationGroups
    {
        None = 0,
        Low,
        High,
        People
    }

    public enum AnimationGroupsType
    {
        Monster = 0,
        SeaMonster,
        Animal,
        Human,
        Equipment,
        Unknown
    }

    public enum HighAnimationGroup
    {
        Walk = 0,
        Stand,
        Die1,
        Die2,
        Attack1,
        Attack2,
        Attack3,
        Misc1,
        Misc2,
        Misc3,
        Stumble,
        SlapGround,
        Cast,
        GetHit1,
        Misc4,
        GetHit2,
        GetHit3,
        Fidget1,
        Fidget2,
        Fly,
        Land,
        DieInFlight,
        AnimationCount
    }

    public enum PeopleAnimationGroup
    {
        WalkUnarmed = 0,
        WalkArmed,
        RunUnarmed,
        RunArmed,
        Stand,
        Fidget1,
        Fidget2,
        StandOnehandedAttack,
        StandTwohandedAttack,
        AttackOnehanded,
        AttackUnarmed1,
        AttackUnarmed2,
        AttackTwohandedDown,
        AttackTwohandedWide,
        AttackTwohandedJab,
        WalkWarmode,
        CastDirected,
        CastArea,
        AttackBow,
        AttackCrossbow,
        GetHit,
        Die1,
        Die2,
        OnmountRideSlow,
        OnmountRideFast,
        OnmountStand,
        OnmountAttack,
        OnmountAttackBow,
        OnmountAttackCrossbow,
        OnmountSlapHorse,
        Turn,
        AttackUnarmedAndWalk,
        EmoteBow,
        EmoteSalute,
        Fidget3,
        AnimationCount
    }

    public enum LowAnimationGroup
    {
        Walk = 0,
        Run,
        Stand,
        Eat,
        Unknown,
        Attack1,
        Attack2,
        Attack3,
        Die1,
        Fidget1,
        Fidget2,
        LieDown,
        Die2,
        AnimationCount
    }

    [Flags]
    public enum AnimationFlags : uint
    {
        None = 0x00000,
        Unknown1 = 0x00001,
        Use2IfHittedWhileRunning = 0x00002,
        IdleAt8Frame = 0x00004,
        CanFlying = 0x00008,
        Unknown10 = 0x00010,
        CalculateOffsetLowGroupExtended = 0x00020,
        CalculateOffsetByLowGroup = 0x00040,
        Unknown80 = 0x00080,
        Unknown100 = 0x00100,
        Unknown200 = 0x00200,
        CalculateOffsetByPeopleGroup = 0x00400,
        Unknown800 = 0x00800,
        Unknown1000 = 0x01000,
        Unknown2000 = 0x02000,
        Unknown4000 = 0x04000,
        Unknown8000 = 0x08000,
        UseUopAnimation = 0x10000,
        Unknown20000 = 0x20000,
        Unknown40000 = 0x40000,
        Unknown80000 = 0x80000,
        Found = 0x80000000
    }

    public struct EquipConvData : IEquatable<EquipConvData>
    {
        public EquipConvData(ushort graphic, ushort gump, ushort color)
        {
            Graphic = graphic;
            Gump = gump;
            Color = color;
        }

        public ushort Graphic;
        public ushort Gump;
        public ushort Color;

        public override int GetHashCode()
        {
            return (Graphic, Gump, Color).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is EquipConvData v && Equals(v);
        }

        public bool Equals(EquipConvData other)
        {
            return (Graphic, Gump, Color) == (other.Graphic, other.Gump, other.Color);
        }
    }

    struct MobTypeInfo
    {
        public AnimationGroupsType Type;
        public AnimationFlags Flags;
    }

    struct BodyInfo
    {
        public ushort Graphic;
        public ushort Hue;
    }

    struct BodyConvInfo
    {
        public int FileIndex;
        public AnimationGroupsType AnimType;
        public ushort Graphic;
        public ushort Hue = INVALID_HUE;
        public sbyte MountHeight;
        public const ushort INVALID_HUE = 0xFF;

        public BodyConvInfo()
        {
        }
    }

    [InlineArray(AnimationsLoader.MAX_ACTIONS)]
    struct ReplacedAnimArray
    {
        private int _a;
    }

    struct UopInfo
    {
        public ReplacedAnimArray ReplacedAnimations;
    }

    [Flags]
    public enum BodyConvFlags
    {
        Anim1 = 0x1,
        Anim2 = 0x2,
        Anim3 = 0x4,
        Anim4 = 0x8,
        Anim5 = 0x10,
    }
}
