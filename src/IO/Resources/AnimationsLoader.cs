#region license

// Copyright (c) 2021, andreakarasho
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.IO.Resources
{
    internal unsafe class AnimationsLoader : UOFileLoader
    {
        private static AnimationsLoader _instance;

        private readonly Dictionary<ushort, byte> _animationSequenceReplacing = new Dictionary<ushort, byte>();
        private readonly AnimationGroup _empty = new AnimationGroup
        {
            Direction = new AnimationDirection[5]
            {
                new AnimationDirection { FileIndex = -1, Address = -1 },
                new AnimationDirection { FileIndex = -1, Address = -1 },
                new AnimationDirection { FileIndex = -1, Address = -1 },
                new AnimationDirection { FileIndex = -1, Address = -1 },
                new AnimationDirection { FileIndex = -1, Address = -1 }
            }
        };
        private readonly Dictionary<ushort, Dictionary<ushort, EquipConvData>> _equipConv = new Dictionary<ushort, Dictionary<ushort, EquipConvData>>();
        private readonly UOFileMul[] _files = new UOFileMul[5];
        private readonly UOFileUop[] _filesUop = new UOFileUop[4];
        private readonly PixelPicker _picker = new PixelPicker();
        private TextureAtlas _atlas;

        private AnimationsLoader()
        {
        }

        public void CreateAtlas(GraphicsDevice device)
        {
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
        }

        public static AnimationsLoader Instance => _instance ?? (_instance = new AnimationsLoader());

        private IndexAnimation[] DataIndex { get; } = new IndexAnimation[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT];

        public IReadOnlyDictionary<ushort, Dictionary<ushort, EquipConvData>> EquipConversions => _equipConv;

        public List<Tuple<ushort, byte>>[] GroupReplaces { get; } = new List<Tuple<ushort, byte>>[2]
        {
            new List<Tuple<ushort, byte>>(), new List<Tuple<ushort, byte>>()
        };

        public override unsafe Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    bool loaduop = false;
                    int[] un = { 0x40000, 0x10000, 0x20000, 0x20000, 0x20000 };

                    for (int i = 0; i < 5; i++)
                    {
                        string pathmul = UOFileManager.GetUOFilePath("anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".mul");

                        string pathidx = UOFileManager.GetUOFilePath("anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".idx");

                        if (File.Exists(pathmul) && File.Exists(pathidx))
                        {
                            _files[i] = new UOFileMul(pathmul, pathidx, un[i], i == 0 ? 6 : -1);
                        }

                        if (i > 0 && Client.IsUOPInstallation)
                        {
                            string pathuop = UOFileManager.GetUOFilePath($"AnimationFrame{i}.uop");

                            if (File.Exists(pathuop))
                            {
                                _filesUop[i - 1] = new UOFileUop(pathuop, "build/animationlegacyframe/{0:D6}/{0:D2}.bin");

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

                    if (Client.Version >= ClientVersion.CV_500A)
                    {
                        string path = UOFileManager.GetUOFilePath("mobtypes.txt");

                        if (File.Exists(path))
                        {
                            string[] typeNames = new string[5]
                            {
                                "monster", "sea_monster", "animal", "human", "equipment"
                            };

                            using (StreamReader reader = new StreamReader(File.OpenRead(path)))
                            {
                                string line;

                                while ((line = reader.ReadLine()) != null)
                                {
                                    line = line.Trim();

                                    if (line.Length == 0 || line[0] == '#' || !char.IsNumber(line[0]))
                                    {
                                        continue;
                                    }

                                    string[] parts = line.Split
                                    (
                                        new[]
                                        {
                                            '\t', ' '
                                        },
                                        StringSplitOptions.RemoveEmptyEntries
                                    );

                                    if (parts.Length < 3)
                                    {
                                        continue;
                                    }

                                    int id = int.Parse(parts[0]);

                                    if (id >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                    {
                                        continue;
                                    }

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
                                        if (testType == typeNames[i])
                                        {
                                            ref IndexAnimation index = ref DataIndex[id];

                                            if (index == null)
                                            {
                                                index = new IndexAnimation();
                                            }

                                            index.Type = (ANIMATION_GROUPS_TYPE) i;
                                            index.Flags = (ANIMATION_FLAGS) (0x80000000 | number);

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    for (ushort i = 0; i < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; i++)
                    {
                        var idxFile = _files[0].IdxFile;

                        if (DataIndex[i] == null)
                        {
                            DataIndex[i] = new IndexAnimation();
                        }

                        if (DataIndex[i].Type == ANIMATION_GROUPS_TYPE.UNKNOWN)
                        {
                            DataIndex[i].Type = CalculateTypeByGraphic(i);
                        }

                        DataIndex[i].Graphic = i;

                        DataIndex[i].CorpseGraphic = i;

                        long offsetToData = DataIndex[i].CalculateOffset(i, DataIndex[i].Type, out int count);
                        long maxaddress = idxFile.StartAddress.ToInt64() + idxFile.Length;

                        if (offsetToData >= idxFile.Length)
                        {
                            continue;
                        }

                        bool isValid = false;

                        long address = idxFile.StartAddress.ToInt64() + offsetToData;

                        DataIndex[i].Groups = new AnimationGroup[100];

                        int offset = 0;

                        for (byte j = 0; j < 100; j++)
                        {
                            DataIndex[i].Groups[j] = new AnimationGroup
                            {
                                Direction = new AnimationDirection[5]
                            };

                            if (j >= count)
                            {
                                continue;
                            }

                            for (byte d = 0; d < 5; d++)
                            {
                                if (DataIndex[i].Groups[j].Direction[d] == null)
                                {
                                    DataIndex[i].Groups[j].Direction[d] = new AnimationDirection();
                                }

                                AnimIdxBlock* aidx = (AnimIdxBlock*) (address + offset * sizeof(AnimIdxBlock));
                                ++offset;

                                if ((long) aidx < maxaddress && aidx->Size != 0 && aidx->Position != 0xFFFFFFFF && aidx->Size != 0xFFFFFFFF)
                                {
                                    DataIndex[i].Groups[j].Direction[d].Address = aidx->Position;
                                    DataIndex[i].Groups[j].Direction[d].Size = aidx->Size;

                                    isValid = true;
                                }
                            }
                        }

                        DataIndex[i].IsValidMUL = isValid;
                    }

                    string file = UOFileManager.GetUOFilePath("Anim1.def");

                    if (File.Exists(file))
                    {
                        using (DefReader defReader = new DefReader(file))
                        {
                            while (defReader.Next())
                            {
                                ushort group = (ushort) defReader.ReadInt();

                                if (group == 0xFFFF)
                                {
                                    continue;
                                }

                                int replace = defReader.ReadGroupInt();

                                GroupReplaces[0].Add(new Tuple<ushort, byte>(group, (byte) replace));
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
                                ushort group = (ushort) defReader.ReadInt();

                                if (group == 0xFFFF)
                                {
                                    continue;
                                }

                                int replace = defReader.ReadGroupInt();

                                GroupReplaces[1].Add(new Tuple<ushort, byte>(group, (byte) replace));
                            }
                        }
                    }

                    if (Client.Version < ClientVersion.CV_300)
                    {
                        return;
                    }

                    ProcessEquipConvDef();
                    ProcessBodyConvDef();
                    ProcessBodyDef();
                    ProcessCorpseDef();
                }
            );
        }

        private void ProcessEquipConvDef()
        {
            var file = UOFileManager.GetUOFilePath("Equipconv.def");

            if (File.Exists(file))
            {
                using (DefReader defReader = new DefReader(file, 5))
                {
                    while (defReader.Next())
                    {
                        ushort body = (ushort)defReader.ReadInt();

                        if (body >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        {
                            continue;
                        }

                        ushort graphic = (ushort)defReader.ReadInt();

                        if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        {
                            continue;
                        }

                        ushort newGraphic = (ushort)defReader.ReadInt();

                        if (newGraphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        {
                            continue;
                        }

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

                        if (!_equipConv.TryGetValue(body, out Dictionary<ushort, EquipConvData> dict))
                        {
                            _equipConv.Add(body, new Dictionary<ushort, EquipConvData>());

                            if (!_equipConv.TryGetValue(body, out dict))
                            {
                                continue;
                            }
                        }

                        dict[graphic] = new EquipConvData(newGraphic, (ushort)gump, color);
                    }
                }
            }

        }

        private void ProcessBodyConvDef()
        {
            var file = UOFileManager.GetUOFilePath("Bodyconv.def");

            if (File.Exists(file))
            {
                using (DefReader defReader = new DefReader(file))
                {
                    while (defReader.Next())
                    {
                        ushort index = (ushort)defReader.ReadInt();

                        if (index >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        {
                            continue;
                        }

                        int[] anim =
                        {
                            defReader.ReadInt(), -1, -1, -1
                        };

                        if (defReader.PartsCount >= 3)
                        {
                            anim[1] = defReader.ReadInt();

                            if (defReader.PartsCount >= 4)
                            {
                                anim[2] = defReader.ReadInt();

                                if (defReader.PartsCount >= 5)
                                {
                                    anim[3] = defReader.ReadInt();
                                }
                            }
                        }

                        int animFile = 0;
                        ushort realAnimID = 0xFFFF;
                        sbyte mountedHeightOffset = 0;

                        if (anim[0] != -1)
                        {
                            animFile = 1;
                            realAnimID = (ushort)anim[0];

                            if (index == 0x00C0 || index == 793)
                            {
                                mountedHeightOffset = -9;
                            }
                        }
                        else if (anim[1] != -1)
                        {
                            animFile = 2;
                            realAnimID = (ushort)anim[1];

                            if (index == 0x0579)
                            {
                                mountedHeightOffset = 9;
                            }
                        }
                        else if (anim[2] != -1)
                        {
                            animFile = 3;
                            realAnimID = (ushort)anim[2];
                        }
                        else if (anim[3] != -1)
                        {
                            animFile = 4;
                            realAnimID = (ushort)anim[3];
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


                        if (realAnimID != 0xFFFF && animFile != 0)
                        {
                            if (_files[animFile] == null)
                            {
                                continue;
                            }

                            UOFile currentIdxFile = _files[animFile].IdxFile;

                            ANIMATION_GROUPS_TYPE realType = Client.Version < ClientVersion.CV_500A ? CalculateTypeByGraphic(realAnimID) : DataIndex[index].Type;

                            long addressOffset = DataIndex[index].CalculateOffset(realAnimID, realType, out int count);

                            if (addressOffset < currentIdxFile.Length)
                            {
                                DataIndex[index].Type = realType;

                                if (DataIndex[index].MountedHeightOffset == 0)
                                {
                                    DataIndex[index].MountedHeightOffset = mountedHeightOffset;
                                }

                                DataIndex[index].GraphicConversion = (ushort)(realAnimID | 0x8000);
                                DataIndex[index].FileIndex = (byte)animFile;

                                addressOffset += currentIdxFile.StartAddress.ToInt64();
                                long maxaddress = currentIdxFile.StartAddress.ToInt64() + currentIdxFile.Length;

                                int offset = 0;

                                DataIndex[index].BodyConvGroups = new AnimationGroup[100];

                                for (int j = 0; j < count; j++)
                                {
                                    DataIndex[index].BodyConvGroups[j] = new AnimationGroup();

                                    if (DataIndex[index].BodyConvGroups[j].Direction == null)
                                    {
                                        DataIndex[index].BodyConvGroups[j].Direction = new AnimationDirection[5];
                                    }

                                    for (byte d = 0; d < 5; d++)
                                    {
                                        if (DataIndex[index].BodyConvGroups[j].Direction[d] == null)
                                        {
                                            DataIndex[index].BodyConvGroups[j].Direction[d] = new AnimationDirection();
                                        }

                                        AnimIdxBlock* aidx = (AnimIdxBlock*)(addressOffset + offset * sizeof(AnimIdxBlock));

                                        ++offset;

                                        if ((long)aidx < maxaddress && /*aidx->Size != 0 &&*/ aidx->Position != 0xFFFFFFFF && aidx->Size != 0xFFFFFFFF)
                                        {
                                            AnimationDirection dataindex = DataIndex[index].BodyConvGroups[j].Direction[d];

                                            dataindex.Address = aidx->Position;
                                            dataindex.Size = Math.Max(1, aidx->Size);
                                            dataindex.FileIndex = animFile;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        private void ProcessBodyDef()
        {
            var file = UOFileManager.GetUOFilePath("Body.def");
            Dictionary<int, bool> filter = new Dictionary<int, bool>();

            if (File.Exists(file))
            {
                using (DefReader defReader = new DefReader(file, 1))
                {
                    while (defReader.Next())
                    {
                        int index = defReader.ReadInt();

                        if (index >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        {
                            continue;
                        }

                        if (filter.TryGetValue(index, out bool b) && b)
                        {
                            continue;
                        }

                        int[] group = defReader.ReadGroup();

                        if (group == null)
                        {
                            continue;
                        }

                        int color = defReader.ReadInt();

                        int checkIndex;

                        //Yes, this is actually how this is supposed to work.
                        if (group.Length >= 3)
                        {
                            checkIndex = group[2];
                        }
                        else
                        {
                            checkIndex = group[0];
                        }

                        if (checkIndex >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        {
                            continue;
                        }

                        DataIndex[index].Graphic = (ushort)checkIndex;

                        DataIndex[index].Color = (ushort)color;

                        DataIndex[index].IsValidMUL = true;

                        filter[index] = true;
                    }
                }
            }

        }

        private void ProcessCorpseDef()
        {
            var file = UOFileManager.GetUOFilePath("Corpse.def");
            Dictionary<int, bool> filter = new Dictionary<int, bool>();

            if (File.Exists(file))
            {
                using (DefReader defReader = new DefReader(file, 1))
                {
                    while (defReader.Next())
                    {
                        int index = defReader.ReadInt();

                        if (index >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        {
                            continue;
                        }

                        if (filter.TryGetValue(index, out bool b) && b)
                        {
                            continue;
                        }

                        int[] group = defReader.ReadGroup();

                        if (group == null)
                        {
                            continue;
                        }

                        int color = defReader.ReadInt();

                        int checkIndex;

                        if (group.Length >= 3)
                        {
                            checkIndex = group[2];
                        }
                        else
                        {
                            checkIndex = group[0];
                        }

                        if (checkIndex >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        {
                            continue;
                        }

                        DataIndex[index].CorpseGraphic = (ushort)checkIndex;

                        DataIndex[index].CorpseColor = (ushort)color;

                        DataIndex[index].IsValidMUL = true;

                        filter[index] = true;
                    }
                }
            }
        }

        private void LoadUop()
        {
            if (Client.Version <= ClientVersion.CV_60144)
            {
                return;
            }

            for (ushort animID = 0; animID < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; animID++)
            {
                for (byte grpID = 0; grpID < 100; grpID++)
                {
                    string hashstring = $"build/animationlegacyframe/{animID:D6}/{grpID:D2}.bin";
                    ulong hash = UOFileUop.CreateHash(hashstring);

                    for (int i = 0; i < _filesUop.Length; i++)
                    {
                        UOFileUop uopFile = _filesUop[i];

                        if (uopFile != null && uopFile.TryGetUOPData(hash, out UOFileIndex data))
                        {
                            if (DataIndex[animID] == null)
                            {
                                DataIndex[animID] = new IndexAnimation
                                {
                                    UopGroups = new AnimationGroupUop[100]
                                };

                                DataIndex[animID].InitializeUOP();
                            }

                            ref AnimationGroupUop g = ref DataIndex[animID].UopGroups[grpID];

                            g = new AnimationGroupUop
                            {
                                Offset = (uint) data.Offset,
                                CompressedLength = (uint) data.Length,
                                DecompressedLength = (uint) data.DecompressedLength,
                                FileIndex = i,
                                Direction = new AnimationDirection[5]
                            };

                            for (int d = 0; d < 5; d++)
                            {
                                if (g.Direction[d] == null)
                                {
                                    g.Direction[d] = new AnimationDirection();
                                }

                                g.Direction[d].IsUOP = true;
                            }
                        }
                    }
                }
            }


            for (int i = 0; i < _filesUop.Length; i++)
            {
                _filesUop[i]?.ClearHashes();
            }

            string animationSequencePath = UOFileManager.GetUOFilePath("AnimationSequence.uop");

            if (!File.Exists(animationSequencePath))
            {
                Log.Warn("AnimationSequence.uop not found");

                return;
            }

            UOFileUop animSeq = new UOFileUop(animationSequencePath, "build/animationsequence/{0:D8}.bin");
            UOFileIndex[] animseqEntries = new UOFileIndex[Math.Max(animSeq.TotalEntriesCount, Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)];
            animSeq.FillEntries(ref animseqEntries);

            Span<byte> spanAlloc = stackalloc byte[1024];

            for (int i = 0; i < animseqEntries.Length; i++)
            {
                ref UOFileIndex entry = ref animseqEntries[i];

                if (entry.Offset == 0)
                {
                    continue;
                }

                animSeq.Seek(entry.Offset);


                byte[] buffer = null;

                Span<byte> span = entry.DecompressedLength <= 1024 ? spanAlloc : (buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(entry.DecompressedLength));
                
                try
                {
                    fixed (byte* destPtr = span)
                    {
                        ZLib.Decompress
                        (
                            animSeq.PositionAddress,
                            entry.Length,
                            0,
                            (IntPtr)destPtr,
                            entry.DecompressedLength
                        );
                    }

                    StackDataReader reader = new StackDataReader(span.Slice(0, entry.DecompressedLength));

                    uint animID = reader.ReadUInt32LE();
                    reader.Skip(48);
                    int replaces = reader.ReadInt32LE();

                    if (replaces != 48 && replaces != 68)
                    {
                        for (int k = 0; k < replaces; k++)
                        {
                            int oldGroup = reader.ReadInt32LE();
                            uint frameCount = reader.ReadUInt32LE();
                            int newGroup = reader.ReadInt32LE();

                            if (frameCount == 0 && DataIndex[animID] != null)
                            {
                                DataIndex[animID].ReplaceUopGroup((byte)oldGroup, (byte)newGroup);
                            }

                            reader.Skip(60);
                        }

                        if (DataIndex[animID] != null)
                        {
                            if (animID == 0x04E7 || animID == 0x042D || animID == 0x04E6 || animID == 0x05F7)
                            {
                                DataIndex[animID].MountedHeightOffset = 18;
                            }
                            else if (animID == 0x01B0 || animID == 0x0579 || animID == 0x05F6 || animID == 0x05A0)
                            {
                                DataIndex[animID].MountedHeightOffset = 9;
                            }
                        }
                    }

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
        public ANIMATION_GROUPS_TYPE GetAnimType(ushort graphic) => DataIndex[graphic].Type;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ANIMATION_FLAGS GetAnimFlags(ushort graphic) => DataIndex[graphic].Flags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte GetMountedHeightOffset(ushort graphic) => DataIndex[graphic].MountedHeightOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint CalculatePeopleGroupOffset(ushort graphic)
        {
            return (uint) (((graphic - 400) * 175 + 35000) * sizeof(AnimIdxBlock));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint CalculateHighGroupOffset(ushort graphic)
        {
            return (uint) (graphic * 110 * sizeof(AnimIdxBlock));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint CalculateLowGroupOffset(ushort graphic)
        {
            return (uint) (((graphic - 200) * 65 + 22000) * sizeof(AnimIdxBlock));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ANIMATION_GROUPS_TYPE CalculateTypeByGraphic(ushort graphic)
        {
            return graphic < 200 ? ANIMATION_GROUPS_TYPE.MONSTER : graphic < 400 ? ANIMATION_GROUPS_TYPE.ANIMAL : ANIMATION_GROUPS_TYPE.HUMAN;
        }

        public void ConvertBodyIfNeeded(ref ushort graphic, bool isParent = false, bool forceUOP = false)
        {
            if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                return;
            }

            IndexAnimation dataIndex = DataIndex[graphic];

            if ((dataIndex.IsUOP && (isParent || !dataIndex.IsValidMUL)) || forceUOP)
            {
                // do nothing ?
            }
            else
            {
                ushort newGraphic = dataIndex.Graphic;

                do
                {
                    if ((DataIndex[newGraphic].HasBodyConversion || !dataIndex.HasBodyConversion) && !(DataIndex[newGraphic].HasBodyConversion && dataIndex.HasBodyConversion))
                    {
                        if (graphic != newGraphic)
                        {
                            graphic = newGraphic;

                            newGraphic = DataIndex[graphic].Graphic;
                        }
                    }
                    else
                    {
                        break;
                    }
                } while (graphic != newGraphic);
            }
        }

        // Do all of the conversion handling for (graphic, action, dir) triples. After they've been through the mapping,
        // they can be used in LoadAnimationFrames() to correctly load the right set of frames.

        public void ReplaceAnimationValues(ref ushort graphic, ref byte action, ref ushort hue, out bool useUOP, bool isEquip = false, bool isCorpse = false, bool forceUOP = false)
        {
            useUOP = false;

            if (graphic < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && action < 100)
            {
                IndexAnimation index = DataIndex[graphic];

                if (forceUOP)
                {
                    index.GetUopGroup(ref action);
                    useUOP = true;
                    return;
                }

                if (index.IsUOP)
                {
                    if (!index.IsValidMUL)
                    {
                        /* Regardless of flags, there is only a UOP version so use that. */
                        index.GetUopGroup(ref action);
                        useUOP = true;
                        return;
                    }

                    /* For equipment, prefer the mul version. */
                    if (!isEquip)
                    {
                        index.GetUopGroup(ref action);
                        useUOP = true;
                        return;
                    }
                }

                ushort newGraphic = isCorpse ? index.CorpseGraphic : index.Graphic;

                do
                {
                    if ((DataIndex[newGraphic].HasBodyConversion || !index.HasBodyConversion) && !(DataIndex[newGraphic].HasBodyConversion && index.HasBodyConversion))
                    {
                        if (graphic != newGraphic)
                        {
                            graphic = newGraphic;
                            if (isCorpse)
                            {
                                hue = index.CorpseColor;
                                newGraphic = DataIndex[graphic].CorpseGraphic;
                            }
                            else
                            {
                                hue = index.Color;
                                newGraphic = DataIndex[graphic].Graphic;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                } while (graphic != newGraphic);
            }
        }

        private AnimationGroup GetAnimationAction(ushort graphic, byte action, bool useUOP)
        {
            if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT || action >= 100)
            {
                return _empty;
            }

            IndexAnimation index = DataIndex[graphic];

            if (useUOP)
            {
                AnimationGroupUop uop = index.GetUopGroup(ref action);

                return uop ?? _empty;
            }

            if (index.HasBodyConversion && index.BodyConvGroups != null)
            {
                return index.BodyConvGroups[action] ?? _empty;
            }

            if (index.Groups != null && index.Groups[action] != null)
            {
                return index.Groups[action] ?? _empty;
            }

            return _empty;
        }

        public override void ClearResources()
        {
        }

        public bool PixelCheck(ushort animID, byte group, byte direction, bool uop, int frame, int x, int y)
        {
            uint packed32 = (uint)((group | (direction << 8) | ((uop ? 0x01 : 0x00) << 16)));
            uint packed32_2 = (uint)((animID | (frame << 16)));
            ulong packed = (packed32_2 | ((ulong)packed32 << 32));

            return _picker.Get(packed, x, y);
        }

        public void UpdateAnimationTable(uint flags)
        {
            for (ushort i = 0; i < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; i++)
            {
                bool replace = DataIndex[i].FileIndex >= 3;

                if (DataIndex[i].FileIndex == 1)
                {
                    replace = (World.ClientLockedFeatures.Flags & LockedFeatureFlags.LordBlackthornsRevenge) != 0;
                }
                else if (DataIndex[i].FileIndex == 2)
                {
                    replace = (World.ClientLockedFeatures.Flags & LockedFeatureFlags.AgeOfShadows) != 0;
                }

                if (replace)
                {
                    if (!DataIndex[i].HasBodyConversion)
                    {
                        DataIndex[i].GraphicConversion = (ushort) (DataIndex[i].GraphicConversion & ~0x8000);
                    }
                }
            }
        }

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
        public void FixSittingDirection(ref byte direction, ref bool mirror, ref int x, ref int y, ref SittingInfoData data)
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
                            direction = (byte) data.Direction4;
                        }
                        else
                        {
                            direction = (byte) data.Direction2;
                        }
                    }
                    else
                    {
                        direction = (byte) data.Direction1;
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
                            direction = (byte) data.Direction1;
                        }
                        else
                        {
                            direction = (byte) data.Direction3;
                        }
                    }
                    else
                    {
                        direction = (byte) data.Direction2;
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
                            direction = (byte) data.Direction2;
                        }
                        else
                        {
                            direction = (byte) data.Direction4;
                        }
                    }
                    else
                    {
                        direction = (byte) data.Direction3;
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
                            direction = (byte) data.Direction3;
                        }
                        else
                        {
                            direction = (byte) data.Direction1;
                        }
                    }
                    else
                    {
                        direction = (byte) data.Direction4;
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
        public ANIMATION_GROUPS GetGroupIndex(ushort graphic)
        {
            if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                return ANIMATION_GROUPS.AG_HIGHT;
            }

            switch (DataIndex[graphic].Type)
            {
                case ANIMATION_GROUPS_TYPE.ANIMAL: return ANIMATION_GROUPS.AG_LOW;

                case ANIMATION_GROUPS_TYPE.MONSTER:
                case ANIMATION_GROUPS_TYPE.SEA_MONSTER: return ANIMATION_GROUPS.AG_HIGHT;

                case ANIMATION_GROUPS_TYPE.HUMAN:
                case ANIMATION_GROUPS_TYPE.EQUIPMENT: return ANIMATION_GROUPS.AG_PEOPLE;
            }

            return ANIMATION_GROUPS.AG_HIGHT;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetDeathAction(ushort id, bool second, bool isRunning = false)
        {
            if (id >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                return 0;
            }

            ANIMATION_FLAGS flags = DataIndex[id].Flags;

            switch (DataIndex[id].Type)
            {
                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    if ((flags & ANIMATION_FLAGS.AF_USE_2_IF_HITTED_WHILE_RUNNING) != 0 || (flags & ANIMATION_FLAGS.AF_CAN_FLYING) != 0)
                    {
                        return 2;
                    }

                    if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                    {
                        return (byte) (second ? 3 : 2);
                    }

                    return (byte) (second ? LOW_ANIMATION_GROUP.LAG_DIE_2 : LOW_ANIMATION_GROUP.LAG_DIE_1);

                case ANIMATION_GROUPS_TYPE.SEA_MONSTER:

                {
                    if (!isRunning)
                    {
                        return 8;
                    }

                    goto case ANIMATION_GROUPS_TYPE.MONSTER;
                }

                case ANIMATION_GROUPS_TYPE.MONSTER:

                    if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                    {
                        return (byte) (second ? 3 : 2);
                    }

                    return (byte) (second ? HIGHT_ANIMATION_GROUP.HAG_DIE_2 : HIGHT_ANIMATION_GROUP.HAG_DIE_1);

                case ANIMATION_GROUPS_TYPE.HUMAN:
                case ANIMATION_GROUPS_TYPE.EQUIPMENT: return (byte) (second ? PEOPLE_ANIMATION_GROUP.PAG_DIE_2 : PEOPLE_ANIMATION_GROUP.PAG_DIE_1);
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAnimationExists(ushort graphic, byte group, bool isCorpse = false)
        {
            if (graphic < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && group < 100)
            {
                ushort hue = 0;

                ReplaceAnimationValues(ref graphic, ref group, ref hue, out var useUOP, false, false, isCorpse);
                AnimationDirection direction = GetAnimationAction(graphic, group, useUOP)?.Direction[0];

                return direction != null && (direction.Address != 0 && direction.Size != 0 || direction.IsUOP);
            }

            return false;
        }

        // Returns the number of frames
        public int LoadAnimationFrames(ushort animID, byte animGroup, byte direction, bool useUOP)
        {
            AnimationDirection animDir = GetAnimationAction(animID, animGroup, useUOP).Direction[direction];

            if (animDir == null || (animDir.FileIndex == -1 && animDir.Address == -1))
            {
                return 0;
            }

            if (animDir.FileIndex >= _files.Length || animID >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                return 0;
            }

            if (animDir.FrameCount > 0 || animDir.SpriteInfos != null)
            {
                // Already loaded
                return animDir.FrameCount;
            }

            Span<FrameInfo> frames;
            int uopFlag = 0;

            if (animDir.IsUOP)
            {
                AnimationGroupUop animData = DataIndex[animID].GetUopGroup(ref animGroup);

                if (animData == null || animData.Offset == 0)
                {
                    return 0;
                }

                frames = ReadUOPAnimationFrames(animID, animGroup, direction);
                uopFlag = 1;
            }
            else if (animDir.Address == 0 && animDir.Size == 0)
            {
                /* If it's not flagged as UOP, but there is no mul data, try to load
                 * it as a UOP anyway. */
                AnimationGroupUop animData = DataIndex[animID].GetUopGroup(ref animGroup);

                if (animData == null || animData.Offset == 0)
                {
                    return 0;
                }

                frames = ReadUOPAnimationFrames(animID, animGroup, direction);
                uopFlag = 1;
            }
            else
            {
                frames = ReadMULAnimationFrames(animID, animGroup, direction, animDir);
            }

            if (frames.Length == 0)
            {
                return 0;
            }

            animDir.FrameCount = (byte)frames.Length;
            animDir.SpriteInfos = new SpriteInfo[frames.Length];

            foreach (var frame in frames)
            {
                if (frame.Width == 0 || frame.Height == 0)
                {
                    /* Missing frame. */
                    continue;
                }

                uint keyUpper = (uint)((animGroup | (direction << 8) | (uopFlag << 16)));
                uint keyLower = (uint)((animID | (frame.Num << 16)));
                ulong key = (keyLower | ((ulong)keyUpper << 32));

                _picker.Set(key, frame.Width, frame.Height, frame.Pixels);

                ref var spriteInfo = ref animDir.SpriteInfos[frame.Num];
                spriteInfo.Center.X = frame.CenterX;
                spriteInfo.Center.Y = frame.CenterY;
                spriteInfo.Texture = _atlas.AddSprite(frame.Pixels.AsSpan(), frame.Width, frame.Height, out spriteInfo.UV);
            }

            return animDir.FrameCount;
        }

        public ref SpriteInfo GetAnimationFrame(ushort id, byte action, byte dir, byte frameNumber, bool useUOP)
        {
            AnimationDirection animDir = GetAnimationAction(id, action, useUOP).Direction[dir];

            if (animDir.FileIndex == -1 && animDir.Address == -1)
            {
                return ref SpriteInfo.Empty;
            }

            if (animDir.FileIndex >= _files.Length || id >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                return ref SpriteInfo.Empty;
            }

            if (frameNumber >= animDir.FrameCount || animDir.SpriteInfos == null)
            {
                return ref SpriteInfo.Empty;
            }

            return ref animDir.SpriteInfos[frameNumber];
        }

        private struct FrameInfo
        {
            public int Num;
            public short CenterX;
            public short CenterY;
            public short Width;
            public short Height;
            public uint[] Pixels;
        }

        [ThreadStatic] private static FrameInfo[] _frames;

        [ThreadStatic] private static byte[] _decompressedData = null;

        private Span<FrameInfo> ReadUOPAnimationFrames(ushort animID, byte animGroup, byte direction)
        {
            AnimationGroupUop animData = DataIndex[animID].GetUopGroup(ref animGroup);

            if (_frames == null)
            {
                _frames = new FrameInfo[22];
            }

            if (animData.FileIndex < 0 || animData.FileIndex >= _filesUop.Length)
            {
                return _frames.AsSpan().Slice(0, 0);
            }

            if (animData.FileIndex == 0 && animData.CompressedLength == 0 && animData.DecompressedLength == 0 && animData.Offset == 0)
            {
                Log.Warn("uop animData is null");

                return _frames.AsSpan().Slice(0, 0);
            }

            int decLen = (int) animData.DecompressedLength;
            UOFileUop file = _filesUop[animData.FileIndex];
            file.Seek(animData.Offset);

            if (_decompressedData == null || decLen > _decompressedData.Length)
            {
                _decompressedData = new byte[decLen];
            }

            fixed (byte* ptr = _decompressedData.AsSpan())
            {
                ZLib.Decompress
                (
                    file.PositionAddress,
                    (int)animData.CompressedLength,
                    0,
                    (IntPtr) ptr,
                    decLen
                );
            }

            StackDataReader reader = new StackDataReader(_decompressedData.AsSpan().Slice(0, decLen));
            reader.Skip(32);

            long end = (long)reader.StartAddress + reader.Length;

            int fc = reader.ReadInt32LE();
            uint dataStart = reader.ReadUInt32LE();
            reader.Seek(dataStart);

            ANIMATION_GROUPS_TYPE type = DataIndex[animID].Type;
            byte frameCount = (byte)(type < ANIMATION_GROUPS_TYPE.EQUIPMENT ? Math.Round(fc / 5f) : 10);
            if (frameCount > _frames.Length)
            {
                _frames = new FrameInfo[frameCount];
            }

            Span<FrameInfo> frames = _frames.AsSpan().Slice(0, frameCount);

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
                        return _frames.AsSpan().Slice(0, 0);
                    }

                    /* FrameID is 1's based and just keeps increasing, regardless of direction.
                     * So north will be 1-22, northeast will be 23-44, etc. And it's possible for frames
                     * to be missing. */
                    ushort headerFrameNum = (ushort)((animHeaderInfo->FrameID - 1) % frameCount);

                    if (frameNum < headerFrameNum)
                    {
                        /* Missing frame. Keep walking forward. */

                        if (currentDir == direction)
                        {
                            /* If the missing frame is for the direction we wanted, make sure
                             * to zero out the entry in the frames array. */
                            frames[frameNum].Num = frameNum;
                            frames[frameNum].CenterX = 0;
                            frames[frameNum].CenterY = 0;
                            frames[frameNum].Width = 0;
                            frames[frameNum].Height = 0;
                        }

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
                            frames[frameNum].Num = frameNum;
                            frames[frameNum].CenterX = 0;
                            frames[frameNum].CenterY = 0;
                            frames[frameNum].Width = 0;
                            frames[frameNum].Height = 0;
                            continue;
                        }
                         reader.Skip((int)animHeaderInfo->DataOffset);

                        ushort* palette = (ushort*)reader.PositionAddress;
                        reader.Skip(512);

                        frames[frameNum].Num = frameNum;
                        ReadSpriteData(ref reader, palette, ref frames[frameNum], true);
                    }

                    reader.Seek(start + sizeof(UOPAnimationHeader));
                }
            }

            reader.Release();

            return frames;
        }

        private Span<FrameInfo> ReadMULAnimationFrames(ushort animID, byte animGroup, byte direction, AnimationDirection animDir)
        {
            UOFile file = _files[animDir.FileIndex];
            StackDataReader reader = new StackDataReader(new ReadOnlySpan<byte>((byte*)file.StartAddress.ToPointer(), (int)file.Length));
            reader.Seek(animDir.Address);

            ushort* palette = (ushort*) reader.PositionAddress;
            reader.Skip(512);

            long dataStart = reader.Position;
            uint frameCount = reader.ReadUInt32LE();
            uint* frameOffset = (uint*)reader.PositionAddress;

            if (_frames == null || frameCount > _frames.Length)
            {
                _frames = new FrameInfo[frameCount];
            }

            Span<FrameInfo> frames = _frames.AsSpan().Slice(0, (int)frameCount);

            for (int i = 0; i < frameCount; i++)
            {
                reader.Seek(dataStart + frameOffset[i]);

                frames[i].Num = i;
                ReadSpriteData(ref reader, palette, ref frames[i], false);
            }

            return frames;
        }

        private void ReadSpriteData(ref StackDataReader reader, ushort* palette, ref FrameInfo frame, bool alphaCheck)
        {
            short imageCenterX = reader.ReadInt16LE();
            short imageCenterY = reader.ReadInt16LE();
            short imageWidth = reader.ReadInt16LE();
            short imageHeight = reader.ReadInt16LE();

            if (imageWidth == 0 || imageHeight == 0)
            {
                return;
            }

            long end = (long)reader.StartAddress + reader.Length;
            int bufferSize = imageWidth * imageHeight;

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
            long pos = reader.Position;

            while (header != 0x7FFF7FFF && pos < end)
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

                x += imageCenterX;
                y += imageCenterY + imageHeight;

                int block = y * imageWidth + x;

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

            frame.Width = imageWidth;
            frame.Height = imageHeight;
            frame.CenterX = imageCenterX;
            frame.CenterY = imageCenterY;
        }

        public void GetAnimationDimensions
        (
            byte animIndex,
            ushort graphic,
            byte dir,
            byte animGroup,
            bool ismounted,
            byte frameIndex,
            out int centerX,
            out int centerY,
            out int width,
            out int height
        )
        {
            dir &= 0x7F;
            bool mirror = false;
            Instance.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
            {
                frameIndex = (byte) animIndex;
            }

            ushort hue = 0;
            ReplaceAnimationValues(ref graphic, ref animGroup, ref hue, out var useUOP, true);
            LoadAnimationFrames(graphic, animGroup, dir, useUOP);
            ref var spriteInfo = ref GetAnimationFrame(graphic, animGroup, dir, frameIndex, useUOP);

            if (spriteInfo.Texture != null)
            {
                centerX = spriteInfo.Center.X;
                centerY = spriteInfo.Center.Y;
                width = spriteInfo.UV.Width;
                height = spriteInfo.UV.Height;
                return;
            }

            centerX = 0;
            centerY = 0;
            width = 0;
            height = ismounted ? 100 : 60;
        }

        public struct SittingInfoData
        {
            public SittingInfoData
            (
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
            public readonly sbyte Direction1, Direction2, Direction3, Direction4;
            public readonly sbyte OffsetY, MirrorOffsetY;
            public readonly bool DrawBack;

            public static SittingInfoData Empty = new SittingInfoData();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private ref struct AnimIdxBlock
        {
            public readonly uint Position;
            public readonly uint Size;
            public readonly uint Unknown;
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

    internal enum ANIMATION_GROUPS
    {
        AG_NONE = 0,
        AG_LOW,
        AG_HIGHT,
        AG_PEOPLE
    }

    internal enum ANIMATION_GROUPS_TYPE
    {
        MONSTER = 0,
        SEA_MONSTER,
        ANIMAL,
        HUMAN,
        EQUIPMENT,
        UNKNOWN
    }

    internal enum HIGHT_ANIMATION_GROUP
    {
        HAG_WALK = 0,
        HAG_STAND,
        HAG_DIE_1,
        HAG_DIE_2,
        HAG_ATTACK_1,
        HAG_ATTACK_2,
        HAG_ATTACK_3,
        HAG_MISC_1,
        HAG_MISC_2,
        HAG_MISC_3,
        HAG_STUMBLE,
        HAG_SLAP_GROUND,
        HAG_CAST,
        HAG_GET_HIT_1,
        HAG_MISC_4,
        HAG_GET_HIT_2,
        HAG_GET_HIT_3,
        HAG_FIDGET_1,
        HAG_FIDGET_2,
        HAG_FLY,
        HAG_LAND,
        HAG_DIE_IN_FLIGHT,
        HAG_ANIMATION_COUNT
    }

    internal enum PEOPLE_ANIMATION_GROUP
    {
        PAG_WALK_UNARMED = 0,
        PAG_WALK_ARMED,
        PAG_RUN_UNARMED,
        PAG_RUN_ARMED,
        PAG_STAND,
        PAG_FIDGET_1,
        PAG_FIDGET_2,
        PAG_STAND_ONEHANDED_ATTACK,
        PAG_STAND_TWOHANDED_ATTACK,
        PAG_ATTACK_ONEHANDED,
        PAG_ATTACK_UNARMED_1,
        PAG_ATTACK_UNARMED_2,
        PAG_ATTACK_TWOHANDED_DOWN,
        PAG_ATTACK_TWOHANDED_WIDE,
        PAG_ATTACK_TWOHANDED_JAB,
        PAG_WALK_WARMODE,
        PAG_CAST_DIRECTED,
        PAG_CAST_AREA,
        PAG_ATTACK_BOW,
        PAG_ATTACK_CROSSBOW,
        PAG_GET_HIT,
        PAG_DIE_1,
        PAG_DIE_2,
        PAG_ONMOUNT_RIDE_SLOW,
        PAG_ONMOUNT_RIDE_FAST,
        PAG_ONMOUNT_STAND,
        PAG_ONMOUNT_ATTACK,
        PAG_ONMOUNT_ATTACK_BOW,
        PAG_ONMOUNT_ATTACK_CROSSBOW,
        PAG_ONMOUNT_SLAP_HORSE,
        PAG_TURN,
        PAG_ATTACK_UNARMED_AND_WALK,
        PAG_EMOTE_BOW,
        PAG_EMOTE_SALUTE,
        PAG_FIDGET_3,
        PAG_ANIMATION_COUNT
    }

    internal enum LOW_ANIMATION_GROUP
    {
        LAG_WALK = 0,
        LAG_RUN,
        LAG_STAND,
        LAG_EAT,
        LAG_UNKNOWN,
        LAG_ATTACK_1,
        LAG_ATTACK_2,
        LAG_ATTACK_3,
        LAG_DIE_1,
        LAG_FIDGET_1,
        LAG_FIDGET_2,
        LAG_LIE_DOWN,
        LAG_DIE_2,
        LAG_ANIMATION_COUNT
    }

    [Flags]
    internal enum ANIMATION_FLAGS : uint
    {
        AF_NONE = 0x00000,
        AF_UNKNOWN_1 = 0x00001,
        AF_USE_2_IF_HITTED_WHILE_RUNNING = 0x00002,
        AF_IDLE_AT_8_FRAME = 0x00004,
        AF_CAN_FLYING = 0x00008,
        AF_UNKNOWN_10 = 0x00010,
        AF_CALCULATE_OFFSET_LOW_GROUP_EXTENDED = 0x00020,
        AF_CALCULATE_OFFSET_BY_LOW_GROUP = 0x00040,
        AF_UNKNOWN_80 = 0x00080,
        AF_UNKNOWN_100 = 0x00100,
        AF_UNKNOWN_200 = 0x00200,
        AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP = 0x00400,
        AF_UNKNOWN_800 = 0x00800,
        AF_UNKNOWN_1000 = 0x01000,
        AF_UNKNOWN_2000 = 0x02000,
        AF_UNKNOWN_4000 = 0x04000,
        AF_UNKNOWN_8000 = 0x08000,
        AF_USE_UOP_ANIMATION = 0x10000,
        AF_UNKNOWN_20000 = 0x20000,
        AF_UNKNOWN_40000 = 0x40000,
        AF_UNKNOWN_80000 = 0x80000,
        AF_FOUND = 0x80000000
    }

    internal class IndexAnimation
    {
        private byte[] _uopReplaceGroupIndex;
        public bool IsUOP => (Flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0;

        public bool HasBodyConversion => (GraphicConversion & 0x8000) == 0 && BodyConvGroups != null;
        public AnimationGroup[] BodyConvGroups;
        public ushort Color;
        public ushort CorpseColor;

        public ushort CorpseGraphic;

        public byte FileIndex;
        public ANIMATION_FLAGS Flags;

        public ushort Graphic;

        public ushort GraphicConversion = 0x8000;

        // 100
        public AnimationGroup[] Groups;

        public bool IsValidMUL;
        public sbyte MountedHeightOffset;

        public ANIMATION_GROUPS_TYPE Type = ANIMATION_GROUPS_TYPE.UNKNOWN;
        public AnimationGroupUop[] UopGroups;


        public AnimationGroupUop GetUopGroup(ref byte group)
        {
            if (group < 100 && UopGroups != null)
            {
                group = _uopReplaceGroupIndex[group];

                return UopGroups[group];
            }

            return  null;
        }

        public void InitializeUOP()
        {
            if (_uopReplaceGroupIndex == null)
            {
                _uopReplaceGroupIndex = new byte[100];

                for (byte i = 0; i < 100; i++)
                {
                    _uopReplaceGroupIndex[i] = i;
                }
            }
        }

        public void ReplaceUopGroup(byte old, byte newG)
        {
            _uopReplaceGroupIndex[old] = newG;
        }

        public long CalculateOffset(ushort graphic, ANIMATION_GROUPS_TYPE type, out int groupCount)
        {
            long result = 0;
            groupCount = 0;

            ANIMATION_GROUPS group = ANIMATION_GROUPS.AG_NONE;

            switch (type)
            {
                case ANIMATION_GROUPS_TYPE.MONSTER:

                    if ((Flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP) != 0)
                    {
                        group = ANIMATION_GROUPS.AG_PEOPLE;
                    }
                    else if ((Flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
                    {
                        group = ANIMATION_GROUPS.AG_LOW;
                    }
                    else
                    {
                        group = ANIMATION_GROUPS.AG_HIGHT;
                    }

                    break;

                case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                    result = AnimationsLoader.CalculateHighGroupOffset(graphic);
                    groupCount = (int) LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;

                    break;

                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    if ((Flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_LOW_GROUP_EXTENDED) != 0)
                    {
                        if ((Flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP) != 0)
                        {
                            group = ANIMATION_GROUPS.AG_PEOPLE;
                        }
                        else if ((Flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
                        {
                            group = ANIMATION_GROUPS.AG_LOW;
                        }
                        else
                        {
                            group = ANIMATION_GROUPS.AG_HIGHT;
                        }
                    }
                    else
                    {
                        group = ANIMATION_GROUPS.AG_LOW;
                    }

                    break;

                default:
                    group = ANIMATION_GROUPS.AG_PEOPLE;

                    break;
            }

            switch (group)
            {
                case ANIMATION_GROUPS.AG_LOW:
                    result = AnimationsLoader.CalculateLowGroupOffset(graphic);
                    groupCount = (int) LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;

                    break;

                case ANIMATION_GROUPS.AG_HIGHT:
                    result = AnimationsLoader.CalculateHighGroupOffset(graphic);
                    groupCount = (int) HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT;

                    break;

                case ANIMATION_GROUPS.AG_PEOPLE:
                    result = AnimationsLoader.CalculatePeopleGroupOffset(graphic);
                    groupCount = (int) PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;

                    break;
            }

            return result;
        }
    }


    internal class AnimationGroup
    {
        public AnimationDirection[] Direction { get; set; }
    }

    internal class AnimationGroupUop : AnimationGroup
    {
        public uint CompressedLength;
        public uint DecompressedLength;
        public int FileIndex;
        public uint Offset;
    }

    internal class AnimationDirection
    {
        public long Address;
        public int FileIndex;
        public byte FrameCount;
        public SpriteInfo[] SpriteInfos;
        //public AnimationFrameTexture[] Frames;
        public bool IsUOP;
        public bool IsVerdata;
        public uint Size;
    }

    struct SpriteInfo
    {
        public Texture2D Texture;
        public Rectangle UV;
        public Point Center;

        public static SpriteInfo Empty = new SpriteInfo { Texture = null };
    }

    internal struct EquipConvData : IEquatable<EquipConvData>
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
}