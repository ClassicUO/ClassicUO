#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public unsafe class AnimationsLoader : UOFileLoader
    {
        public const int MAX_ACTIONS = 80; // gargoyle is like 78
        public const int MAX_DIRECTIONS = 5;

        private static AnimationsLoader _instance;

        [ThreadStatic]
        private static FrameInfo[] _frames;

        [ThreadStatic]
        private static byte[] _decompressedData;

        private readonly UOFileMul[] _files = new UOFileMul[5];
        private readonly UOFileUop[] _filesUop = new UOFileUop[4];

        private readonly Dictionary<ushort, Dictionary<ushort, EquipConvData>> _equipConv = new Dictionary<ushort, Dictionary<ushort, EquipConvData>>();
        private readonly Dictionary<int, MobTypeInfo> _mobTypes = new Dictionary<int, MobTypeInfo>();
        private readonly Dictionary<int, BodyInfo> _bodyInfos = new Dictionary<int, BodyInfo>();
        private readonly Dictionary<int, BodyInfo> _corpseInfos = new Dictionary<int, BodyInfo>();
        private readonly Dictionary<int, BodyConvInfo> _bodyConvInfos = new Dictionary<int, BodyConvInfo>();
        private readonly Dictionary<int, UopInfo> _uopInfos = new Dictionary<int, UopInfo>();

        private AnimationsLoader() { }

        public static AnimationsLoader Instance =>
            _instance ?? (_instance = new AnimationsLoader());

        public IReadOnlyDictionary<ushort, Dictionary<ushort, EquipConvData>> EquipConversions =>  _equipConv;

        public List<(ushort, byte)>[] GroupReplaces { get; } =
            new List<(ushort, byte)>[2]
            {
                new List<(ushort, byte)>(),
                new List<(ushort, byte)>()
            };

        private unsafe void LoadInternal()
        {
            bool loaduop = false;
            int[] un = { 0x40000, 0x10000, 0x20000, 0x20000, 0x20000 };

            for (int i = 0; i < 5; i++)
            {
                string pathmul = UOFileManager.GetUOFilePath(
                    "anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".mul"
                );

                string pathidx = UOFileManager.GetUOFilePath(
                    "anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".idx"
                );

                if (File.Exists(pathmul) && File.Exists(pathidx))
                {
                    _files[i] = new UOFileMul(pathmul, pathidx, un[i], i == 0 ? 6 : -1);
                }

                if (i > 0 && UOFileManager.IsUOPInstallation)
                {
                    string pathuop = UOFileManager.GetUOFilePath($"AnimationFrame{i}.uop");

                    if (File.Exists(pathuop))
                    {
                        _filesUop[i - 1] = new UOFileUop(
                            pathuop,
                            "build/animationlegacyframe/{0:D6}/{0:D2}.bin"
                        );

                        if (!loaduop)
                        {
                            loaduop = true;
                        }
                    }
                }
            }

            if (loaduop)
            {
                LoadUop();
            }

            if (UOFileManager.Version >= ClientVersion.CV_500A)
            {
                string path = UOFileManager.GetUOFilePath("mobtypes.txt");

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
                                        Flags = (AnimationFlags )(0x80000000 | number)
                                    };

                                    break;
                                }
                            }
                        }
                    }
                }
            }

            string file = UOFileManager.GetUOFilePath("Anim1.def");

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

            file = UOFileManager.GetUOFilePath("Anim2.def");

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

        public ReadOnlySpan<AnimIdxBlock> GetIndices
        (
            ClientVersion clientVersion,
            ushort body,
            ref ushort hue,
            ref AnimationFlags  flags,
            out int fileIndex,
            out AnimationGroupsType animType,
            out sbyte mountHeight
        )
        {
            fileIndex = 0;
            animType = AnimationGroupsType.Unknown;
            mountHeight = 0;

            if (!_mobTypes.TryGetValue(body, out var mobInfo))
            {
                mobInfo.Flags = AnimationFlags.None;
                mobInfo.Type = AnimationGroupsType.Unknown;
            }

            flags = mobInfo.Flags;

            if (mobInfo.Flags.HasFlag(AnimationFlags.UseUopAnimation))
            {
                if (animType == AnimationGroupsType.Unknown)
                    animType = mobInfo.Type != AnimationGroupsType.Unknown ? mobInfo.Type : CalculateTypeByGraphic(body);

                var replaceFound = _uopInfos.TryGetValue(body, out var uopInfo);
                mountHeight = uopInfo.HeightOffset;
                var animIndices = Array.Empty<AnimIdxBlock>();

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
                                animIndices = new AnimIdxBlock[MAX_ACTIONS];

                            fileIndex = index;

                            ref var animIndex = ref animIndices[actioIdx];
                            animIndex.Position = (uint)data.Offset;
                            animIndex.Size = (uint)data.Length;
                            animIndex.Unknown = (uint)data.DecompressedLength;

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
                mountHeight = bodyConvInfo.MountHeight;

                if (clientVersion < ClientVersion.CV_500A)
                    animType = bodyConvInfo.AnimType;
            }

            if (animType == AnimationGroupsType.Unknown)
                animType = mobInfo.Type != AnimationGroupsType.Unknown ? mobInfo.Type : CalculateTypeByGraphic(body, fileIndex);

            var fileIdx = _files[fileIndex].IdxFile;
            var offsetAddress = CalculateOffset(body, animType, flags, out var actionCount);

            var offset = fileIdx.StartAddress.ToInt64() + offsetAddress;
            var end = fileIdx.StartAddress.ToInt64() + fileIdx.Length;

            if (offset >= end)
            {
                return ReadOnlySpan<AnimIdxBlock>.Empty;
            }

            if (offset + (actionCount * MAX_DIRECTIONS * sizeof(AnimIdxBlock)) > end)
            {
                return ReadOnlySpan<AnimIdxBlock>.Empty;
            }

            var animIdxSpan = new ReadOnlySpan<AnimIdxBlock>(
                (void*)offset,
                actionCount * MAX_DIRECTIONS
            );

            return animIdxSpan;
        }

        private long CalculateOffset(
            ushort graphic,
            AnimationGroupsType type,
            AnimationFlags  flags,
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

        public override unsafe Task Load()
        {
            return Task.Run(LoadInternal);
        }

        private void ProcessEquipConvDef()
        {
            if (UOFileManager.Version < ClientVersion.CV_300)
            {
                return;
            }

            var file = UOFileManager.GetUOFilePath("Equipconv.def");

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
            if (UOFileManager.Version < ClientVersion.CV_300)
            {
                return;
            }

            var file = UOFileManager.GetUOFilePath("Bodyconv.def");

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
            if (UOFileManager.Version < ClientVersion.CV_300)
            {
                return;
            }

            var file = UOFileManager.GetUOFilePath("Body.def");

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
            if (UOFileManager.Version < ClientVersion.CV_300)
            {
                return;
            }

            var file = UOFileManager.GetUOFilePath("Corpse.def");

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
            if (UOFileManager.Version <= ClientVersion.CV_60144)
            {
                return;
            }

            string animationSequencePath = UOFileManager.GetUOFilePath("AnimationSequence.uop");

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
            //var animseqEntries = new UOFileIndex[animSeq.TotalEntriesCount];
            //animSeq.FillEntries(ref animseqEntries);

            Span<byte> spanAlloc = stackalloc byte[1024];

            foreach (var pair in animSeq.Hashes)
            {
                var entry = pair.Value;

                if (entry.Offset == 0)
                {
                    continue;
                }

                animSeq.Seek(entry.Offset);

                byte[] buffer = null;

                Span<byte> span =
                    entry.DecompressedLength <= 1024
                        ? spanAlloc
                        : (
                            buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(
                                entry.DecompressedLength
                            )
                        );

                try
                {
                    fixed (byte* destPtr = span)
                    {
                        var result = ZLib.Decompress(
                            animSeq.PositionAddress,
                            entry.Length,
                            0,
                            (IntPtr)destPtr,
                            entry.DecompressedLength
                        );

                        if (result != ZLib.ZLibError.Okay)
                        {
                            Log.Error($"error reading animationsequence {result}");
                            return;
                        }
                    }

                    var reader = new StackDataReader(span.Slice(0, entry.DecompressedLength));

                    uint animID = reader.ReadUInt32LE();
                    reader.Skip(48);
                    int replaces = reader.ReadInt32LE();

                    var uopInfo = new UopInfo();
                    var replacedAnimSpan = uopInfo.ReplacedAnimations;
                    for (var j = 0; j < replacedAnimSpan.Length; ++j)
                        replacedAnimSpan[j] = j;

                    if (replaces != 48 && replaces != 68)
                    {
                        for (int k = 0; k < replaces; k++)
                        {
                            int oldGroup = reader.ReadInt32LE();
                            uint frameCount = reader.ReadUInt32LE();
                            int newGroup = reader.ReadInt32LE();

                            if (frameCount == 0)
                            {
                                replacedAnimSpan[oldGroup] = newGroup;
                            }

                            reader.Skip(60);
                        }

                        if (
                            animID == 0x04E7
                            || animID == 0x042D
                            || animID == 0x04E6
                            || animID == 0x05F7
                            || animID == 0x05A1
                        )
                        {
                            uopInfo.HeightOffset = 18;
                        }
                        else if (
                            animID == 0x01B0
                            || animID == 0x0579
                            || animID == 0x05F6
                            || animID == 0x05A0
                        )
                        {
                            uopInfo.HeightOffset = 9;
                        }
                    }

                    _uopInfos[(int)animID] = uopInfo;

                    reader.Release();
                }
                finally
                {
                    if (buffer != null)
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
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
        public void GetAnimDirection(ref byte dir, ref bool mirror)
        {
            switch (dir)
            {
                case 2:
                case 4:
                    mirror = dir == 2;
                    dir = 1;

                    break;

                case 1:
                case 5:
                    mirror = dir == 1;
                    dir = 2;

                    break;

                case 0:
                case 6:
                    mirror = dir == 0;
                    dir = 3;

                    break;

                case 3:
                    dir = 0;

                    break;

                case 7:
                    dir = 4;

                    break;
            }
        }

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
            AnimationFlags  animFlags,
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
            AnimationsLoader.AnimIdxBlock index
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
                && index.Unknown == 0
                && index.Position == 0
            )
            {
                Log.Warn("uop animData is null");

                return Span<FrameInfo>.Empty;
            }

            file.Seek(index.Position);

            if (_decompressedData == null || index.Unknown > _decompressedData.Length)
            {
                _decompressedData = new byte[index.Unknown];
            }

            fixed (byte* ptr = _decompressedData.AsSpan())
            {
                var result = ZLib.Decompress(
                    file.PositionAddress,
                    (int)index.Size,
                    0,
                    (IntPtr)ptr,
                    (int)index.Unknown
                );

                if (result != ZLib.ZLibError.Okay)
                {
                    Log.Error($"error reading uop animation. AnimID: {animID} | Group: {animGroup} | Dir: {direction} | FileIndex: {fileIndex}");

                    return Span<FrameInfo>.Empty;
                }
            }

            var reader = new StackDataReader(
                _decompressedData.AsSpan().Slice(0, (int)index.Unknown)
            );
            reader.Skip(32);

            long end = (long)reader.StartAddress + reader.Length;

            int fc = reader.ReadInt32LE();
            uint dataStart = reader.ReadUInt32LE();
            reader.Seek(dataStart);

            byte frameCount = (byte)(
                type < AnimationGroupsType.Equipment ? Math.Round(fc / (float) MAX_DIRECTIONS) : MAX_DIRECTIONS * 2
            );
            if (frameCount > _frames.Length)
            {
                _frames = new FrameInfo[frameCount];
            }

            var frames = _frames.AsSpan(0, frameCount);

            /* If the UOP files didn't omit frames, we could just do this:
             * reader.Skip(sizeof(UOPAnimationHeader) * direction * frameCount);
             * but we can't. So we have to walk through the frames to seek to where we need to go.
             */
            UOPAnimationHeader* animHeaderInfo = (UOPAnimationHeader*)reader.PositionAddress;

            for (ushort currentDir = 0; currentDir <= direction; currentDir++)
            {
                for (ushort frameNum = 0; frameNum < frameCount; frameNum++)
                {
                    long start = reader.Position;
                    animHeaderInfo = (UOPAnimationHeader*)reader.PositionAddress;

                    if (animHeaderInfo->Group != animGroup)
                    {
                        /* Something bad has happened. Just return. */
                        return Span<FrameInfo>.Empty;
                    }

                    /* FrameID is 1's based and just keeps increasing, regardless of direction.
                     * So north will be 1-22, northeast will be 23-44, etc. And it's possible for frames
                     * to be missing. */
                    ushort headerFrameNum = (ushort)((animHeaderInfo->FrameID - 1) % frameCount);

                    ref var frame = ref frames[frameNum];

                    // we need to zero-out the frame or we will see ghost animations coming from other animation queries
                    frame.Num = frameNum;
                    frame.CenterX = 0;
                    frame.CenterY = 0;
                    frame.Width = 0;
                    frame.Height = 0;

                    if (frameNum < headerFrameNum)
                    {
                        /* Missing frame. Keep walking forward. */
                        continue;
                    }

                    if (frameNum > headerFrameNum)
                    {
                        /* We've reached the next direction early */
                        break;
                    }

                    if (currentDir == direction)
                    {
                        /* We're on the direction we actually wanted to read */
                        if (start + animHeaderInfo->DataOffset >= reader.Length)
                        {
                            /* File seems to be corrupt? Skip loading. */
                            continue;
                        }

                        reader.Skip((int)animHeaderInfo->DataOffset);

                        var palette = new ReadOnlySpan<ushort>(reader.PositionAddress.ToPointer(), 512 / sizeof(ushort));
                        reader.Skip(512);

                        ReadSpriteData(ref reader, palette, ref frame, true);
                    }

                    reader.Seek(start + sizeof(UOPAnimationHeader));
                }
            }

            reader.Release();

            return frames;
        }

        public Span<FrameInfo> ReadMULAnimationFrames(int fileIndex, AnimIdxBlock index)
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
            
            var reader = new StackDataReader(
                new ReadOnlySpan<byte>(
                    (byte*)file.StartAddress.ToPointer() + index.Position,
                    (int)index.Size
                )
            );
            
            reader.Seek(0);

            var palette = new ReadOnlySpan<ushort>(reader.PositionAddress.ToPointer(), 512 / sizeof(ushort));
            reader.Skip(512);

            long dataStart = reader.Position;
            uint frameCount = reader.ReadUInt32LE();
            var frameOffset = new ReadOnlySpan<uint>((uint*)reader.PositionAddress, (int)frameCount);

            if (_frames == null || frameCount > _frames.Length)
            {
                _frames = new FrameInfo[frameCount];
            }

            var frames = _frames.AsSpan().Slice(0, (int)frameCount);

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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        ref struct UOPAnimationHeader
        {
            public ushort Group;
            public ushort FrameID;

            public ushort Unk0;
            public ushort Unk1;
            public ushort Unk2;
            public ushort Unk3;

            public uint DataOffset;
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

    unsafe struct UopInfo
    {
        private fixed int _replacedAnim[AnimationsLoader.MAX_ACTIONS];

        public Span<int> ReplacedAnimations
        {
            get
            {

                fixed (int* ptr = _replacedAnim)
                {
                    return new Span<int>(ptr, AnimationsLoader.MAX_ACTIONS);
                }
            }
        }

        public sbyte HeightOffset;
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
