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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClassicUO;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
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

        private readonly AnimationCache _animationCache = new AnimationCache(Client.IsUOPInstallation);
        private readonly Dictionary<ushort, Rectangle> _animDimensionCache = new Dictionary<ushort, Rectangle>();
        private readonly Dictionary<ushort, Dictionary<ushort, EquipConvData>> _equipConv = new Dictionary<ushort, Dictionary<ushort, EquipConvData>>();
        private readonly UOFileMul[] _files = new UOFileMul[5];
        private readonly UOFileUop[] _filesUop = new UOFileUop[4];
        private readonly PixelPicker _picker = new PixelPicker();

        private readonly LinkedList<ulong> _usedTextures = new LinkedList<ulong>();

        private AnimationsLoader()
        {
        }

        public static AnimationsLoader Instance => _instance ?? (_instance = new AnimationsLoader());

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

                    int animIdxBlockSize = sizeof(AnimIdxBlock);

                    UOFile idxfile0 = _files[0]?.IdxFile;

                    long? maxAddress0 = (long?) idxfile0?.StartAddress + idxfile0?.Length;

                    UOFile idxfile2 = _files[1]?.IdxFile;

                    long? maxAddress2 = (long?) idxfile2?.StartAddress + idxfile2?.Length;

                    UOFile idxfile3 = _files[2]?.IdxFile;

                    long? maxAddress3 = (long?) idxfile3?.StartAddress + idxfile3?.Length;

                    UOFile idxfile4 = _files[3]?.IdxFile;

                    long? maxAddress4 = (long?) idxfile4?.StartAddress + idxfile4?.Length;

                    UOFile idxfile5 = _files[4]?.IdxFile;

                    long? maxAddress5 = (long?) idxfile5?.StartAddress + idxfile5?.Length;

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
                                            ref var index = ref _animationCache.GetEntry(id);

                                            index.Type = (ANIMATION_GROUPS_TYPE) (i + 1);
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
                        ref var entry = ref _animationCache.GetEntry(i);

                        if (entry.Type == ANIMATION_GROUPS_TYPE.UNKNOWN)
                        {
                            entry.Type = CalculateTypeByGraphic(i);
                        }

                        entry.Graphic = i;
                        entry.CorpseGraphic = i;
                        entry.GraphicConversion = 0x8000;

                        long offsetToData = CalculateOffset(i, entry.Flags, entry.Type, out int count);

                        if (offsetToData >= idxfile0.Length)
                        {
                            continue;
                        }

                        bool isValid = false;

                        long address = _files[0].IdxFile.StartAddress.ToInt64() + offsetToData;

                        int offset = 0;

                        for (byte j = 0; j < count; j++)
                        {
                            for (byte d = 0; d < 5; d++)
                            {
                                AnimIdxBlock* aidx = (AnimIdxBlock*) (address + offset * animIdxBlockSize);
                                ++offset;

                                if ((long) aidx < maxAddress0 && aidx->Size != 0 && aidx->Position != 0xFFFFFFFF && aidx->Size != 0xFFFFFFFF)
                                {
                                    ref var dirEntry = ref _animationCache.GetDirectionEntry(i, j, d);

                                    dirEntry.Address = (IntPtr) aidx->Position;
                                    dirEntry.Size = aidx->Size;

                                    isValid = true;
                                }
                            }
                        }

                        entry.IsValidMUL = isValid;
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

                    file = UOFileManager.GetUOFilePath("Equipconv.def");

                    if (File.Exists(file))
                    {
                        using (DefReader defReader = new DefReader(file, 5))
                        {
                            while (defReader.Next())
                            {
                                ushort body = (ushort) defReader.ReadInt();

                                if (body >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                {
                                    continue;
                                }

                                ushort graphic = (ushort) defReader.ReadInt();

                                if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                {
                                    continue;
                                }

                                ushort newGraphic = (ushort) defReader.ReadInt();

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

                                ushort color = (ushort) defReader.ReadInt();

                                if (!_equipConv.TryGetValue(body, out Dictionary<ushort, EquipConvData> dict))
                                {
                                    _equipConv.Add(body, new Dictionary<ushort, EquipConvData>());

                                    if (!_equipConv.TryGetValue(body, out dict))
                                    {
                                        continue;
                                    }
                                }

                                dict[graphic] = new EquipConvData(newGraphic, (ushort) gump, color);
                            }
                        }
                    }

                    file = UOFileManager.GetUOFilePath("Bodyconv.def");

                    if (File.Exists(file))
                    {
                        using (DefReader defReader = new DefReader(file))
                        {
                            while (defReader.Next())
                            {
                                ushort index = (ushort) defReader.ReadInt();

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

                                if (anim[0] != -1 && maxAddress2.HasValue && maxAddress2 != 0)
                                {
                                    animFile = 1;
                                    realAnimID = (ushort) anim[0];

                                    if (index == 0x00C0 || index == 793)
                                    {
                                        mountedHeightOffset = -9;
                                    }
                                }
                                else if (anim[1] != -1 && maxAddress3.HasValue && maxAddress3 != 0)
                                {
                                    animFile = 2;
                                    realAnimID = (ushort) anim[1];

                                    if (index == 0x0579)
                                    {
                                        mountedHeightOffset = 9;
                                    }
                                }
                                else if (anim[2] != -1 && maxAddress4.HasValue && maxAddress4 != 0)
                                {
                                    animFile = 3;
                                    realAnimID = (ushort) anim[2];
                                }
                                else if (anim[3] != -1 && maxAddress5.HasValue && maxAddress5 != 0)
                                {
                                    animFile = 4;
                                    realAnimID = (ushort) anim[3];
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
                                    UOFile currentIdxFile = _files[animFile].IdxFile;

                                    ref var animIndex = ref _animationCache.GetEntry(index);

                                    ANIMATION_GROUPS_TYPE realType = Client.Version < ClientVersion.CV_500A ?
                                        CalculateTypeByGraphic(realAnimID) :
                                        animIndex.Type;

                                    long addressOffset = CalculateOffset(realAnimID, animIndex.Flags, realType, out int count);

                                    if (addressOffset < currentIdxFile.Length)
                                    {
                                        animIndex.Type = realType;

                                        if (animIndex.MountOffsetY == 0)
                                        {
                                            animIndex.MountOffsetY = mountedHeightOffset;
                                        }

                                        animIndex.GraphicConversion = (ushort) (realAnimID | 0x8000);
                                        animIndex.FileIndex = (byte) animFile;

                                        addressOffset += currentIdxFile.StartAddress.ToInt64();
                                        long maxaddress = currentIdxFile.StartAddress.ToInt64() + currentIdxFile.Length;

                                        int offset = 0;

                                        for (byte j = 0; j < count; j++)
                                        {  
                                            for (byte d = 0; d < 5; d++)
                                            {
                                                AnimIdxBlock* aidx = (AnimIdxBlock*) (addressOffset + offset * animIdxBlockSize);

                                                ++offset;

                                                if ((long) aidx < maxaddress /*&& aidx->Size != 0*/ && aidx->Position != 0xFFFFFFFF &&
                                                    aidx->Size != 0xFFFFFFFF)
                                                {
                                                    //ref var convertedEntry = ref _animationCache.GetConvertedDirectionEntry(index, j, d);

                                                    ref AnimationDirectionEntry convertedEntry = ref _animationCache.GetDirectionEntry(index, j, d, false, true);

                                                    convertedEntry.Address = (IntPtr) aidx->Position;
                                                    convertedEntry.Size = Math.Min(1, aidx->Size);
                                                    convertedEntry.FileIndex = (byte) animFile;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    file = UOFileManager.GetUOFilePath("Body.def");
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

                                ref var entry = ref _animationCache.GetEntry(index);

                                entry.Graphic = (ushort) checkIndex;
                                entry.Color = (ushort) color;
                                entry.IsValidMUL = true;
                                
                                filter[index] = true;
                            }
                        }
                    }

                    file = UOFileManager.GetUOFilePath("Corpse.def");
                    filter.Clear();

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

                                ref var entry = ref _animationCache.GetEntry(index);

                                entry.CorpseGraphic = (ushort) checkIndex;
                                entry.CorpseColor = (ushort) color;
                                entry.IsValidMUL = true;

                                filter[index] = true;
                            }
                        }
                    }
                }
            );
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

                    for (byte i = 0; i < _filesUop.Length; i++)
                    {
                        UOFileUop uopFile = _filesUop[i];

                        if (uopFile != null && uopFile.TryGetUOPData(hash, out UOFileIndex data))
                        {
                            _animationCache.ReplaceUopGroup(animID, grpID, grpID);

                            ref var entry = ref _animationCache.GetEntry(animID);
                            entry.Graphic = animID;
                            entry.CorpseGraphic = animID;
                            entry.GraphicConversion = 0x8000;
                            entry.FileIndex = i;
                            entry.Flags |= ANIMATION_FLAGS.AF_USE_UOP_ANIMATION | ANIMATION_FLAGS.AF_FOUND;

                            for (byte d = 0; d < 5; d++)
                            {
                                ref var dirEntry = ref _animationCache.GetDirectionEntry(animID, grpID, d, true);
                                dirEntry.FileIndex = i;
                                dirEntry.Address = uopFile.StartAddress;
                                dirEntry.Size = (uint) uopFile.Length;
                            }
                        }
                    }
                }
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

            for (int i = 0; i < animseqEntries.Length; i++)
            {
                ref UOFileIndex entry = ref animseqEntries[i];

                if (entry.Offset == 0)
                {
                    continue;
                }

                animSeq.Seek(entry.Offset);

                byte[] decbuffer = animSeq.GetData(entry.Length, entry.DecompressedLength);

                try
                {
                    StackDataReader reader = new StackDataReader(new ReadOnlySpan<byte>(decbuffer).Slice(0, entry.DecompressedLength));

                    uint animID = reader.ReadUInt32LE();
                    reader.Skip(48);
                    int replaces = reader.ReadInt32LE();

                if (replaces != 48 && replaces != 68)
                {
                    ref var animEntry = ref _animationCache.GetEntry((int)animID);

                    if ((animEntry.Flags & ANIMATION_FLAGS.AF_FOUND) != 0 && (animEntry.Flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) == 0)
                    {
                        animEntry.Flags |= ANIMATION_FLAGS.AF_USE_UOP_ANIMATION;
                    }

                    for (int k = 0; k < replaces; k++)
                    {
                        int oldGroup = reader.ReadInt32LE();
                        uint frameCount = reader.ReadUInt32LE();
                        int newGroup = reader.ReadInt32LE();


                        if (frameCount == 0)
                        {
                            if (oldGroup >= 0 && newGroup >= 0)
                            {
                                _animationCache.ReplaceUopGroup((ushort)animID, (byte)oldGroup, (byte)newGroup);
                            }
                        }
                        else
                        {
                            /*if (oldGroup >= 0)
                            {
                                for (int j = k; j < 5; ++j)
                                {
                                    ref var group = ref _animationCache.GetDirectionEntry((ushort)animID, (byte)oldGroup, (byte)j, true);

                                    group.RealFrameCount = (byte)frameCount;
                                }
                            }*/
                        }

                        reader.Skip(60);
                    }

                    if (animEntry.Graphic != 0)
                    {
                        if (animID == 0x04E7 || animID == 0x042D || animID == 0x04E6 || animID == 0x05F7)
                        {
                            animEntry.MountOffsetY = 18;
                        }
                        else if (animID == 0x01B0 || animID == 0x0579 || animID == 0x05F6 || animID == 0x05A0)
                        {
                            animEntry.MountOffsetY = 9;
                        }
                    }
                }

                    reader.Release();
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(decbuffer);
                }
            }

            animSeq.Dispose();
        }


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

        public ref AnimationEntry GetAnimationEntry(ushort graphic) => ref _animationCache.GetEntry(graphic);

        private ref AnimationDirectionEntry GetAnimationDirectionEntry(ushort graphic, byte group, byte direction, bool isUop = false)
        {
            ref AnimationEntry entry = ref _animationCache.GetEntry(graphic);

            if (!isUop && (entry.Flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
            {
                isUop = true;
            }

            bool conv = HasBodyConversion(ref entry);

            ref AnimationDirectionEntry dirEntry = ref _animationCache.GetDirectionEntry(graphic, group, direction, isUop, conv);

            if (dirEntry.Address == IntPtr.Zero)
            {
                if (conv)
                {
                    dirEntry = ref _animationCache.GetDirectionEntry(graphic, group, direction, isUop, false);
                }
            }

            if (dirEntry.FramesCount == 0 && dirEntry.Address != IntPtr.Zero)
            {
                if (LoadAnimationFrames(graphic, group, direction, isUop, ref dirEntry))
                {

                }
            }

            return ref dirEntry;
        }

        private static bool HasBodyConversion(ref AnimationEntry entry)
        {
            return (entry.GraphicConversion & 0x8000) == 0;
        }

        public byte GetFrameInfo(ushort graphic, byte group, byte direction, bool forceUop = false)
        {
            ushort hue = 0;
            FixAnimationGraphicAndHue(ref graphic, ref hue, false, false, forceUop, out _);
            ref AnimationDirectionEntry dirEntry = ref GetAnimationDirectionEntry(graphic, group, direction, forceUop);

            if (dirEntry.FramesCount != 0)
            {
                dirEntry.LastAccessTime = Time.Ticks;
                return dirEntry.FramesCount;
            }

            return 0;
        }

        public AnimationFrameTexture GetBodyFrame(ref ushort graphic, ref ushort hue, byte group, byte direction, int frame, bool isParent = false, bool forceUOP = false)
        {
            return GetFrame
            (
                ref graphic,
                ref hue,
                group,
                direction,
                frame,
                false,
                isParent,
                forceUOP
            );
        }

        public AnimationFrameTexture GetCorpseFrame(ref ushort graphic, ref ushort hue, byte group, byte direction, int frame, bool forceUOP = false)
        {
            return GetFrame
            (
                ref graphic,
                ref hue,
                group,
                direction,
                frame,
                true,
                false,
                forceUOP
            );
        }

        public void FixAnimationGraphicAndHue(ref ushort graphic, ref ushort hue, bool isCorpse, bool isParent, bool forceUOP, out bool isUop)
        {
            ref var entry = ref _animationCache.GetEntry(graphic);

            isUop = forceUOP || ((entry.Flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0 && (isParent || !entry.IsValidMUL));

            if (!isUop)
            {
                ushort newGraphic = isCorpse ? entry.CorpseGraphic : entry.Graphic;

                do
                {
                    ref var newEntry = ref _animationCache.GetEntry(newGraphic);

                    bool convert = (HasBodyConversion(ref newEntry) || !HasBodyConversion(ref entry))
                                   &&
                                   !(HasBodyConversion(ref newEntry) && HasBodyConversion(ref entry));

                    if (convert)
                    {
                        if (graphic != newGraphic)
                        {
                            graphic = newGraphic;

                            ref var ent = ref _animationCache.GetEntry(graphic);

                            newGraphic = isCorpse ? entry.CorpseGraphic : ent.Graphic;

                            if (hue == 0)
                            {
                                hue = isCorpse ? entry.CorpseColor : entry.Color;
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

        private AnimationFrameTexture GetFrame
        (
            ref ushort graphic,
            ref ushort hue,
            byte group,
            byte direction,
            int frame,
            bool isCorpse,
            bool isParent,
            bool forceUOP
        )
        {
            FixAnimationGraphicAndHue
            (
                ref graphic,
                ref hue,
                isCorpse,
                isParent,
                forceUOP,
                out bool isUop
            );

            ref AnimationDirectionEntry dirEntry = ref GetAnimationDirectionEntry(graphic, group, direction, isUop);

            if (dirEntry.FramesCount != 0)
            {
                dirEntry.LastAccessTime = Time.Ticks;
            }

            return _animationCache.GetFrame(graphic, group, direction, frame, isUop);
        }


        public override void ClearResources()
        {
            LinkedListNode<ulong> first = _usedTextures.First;

            while (first != null)
            {
                var next = first.Next;

                ulong value = first.Value;

                ushort animID = (ushort)(0xFFFF_FFFF & value);
                uint unpacked32 = (uint)(value >> 32);

                byte group = (byte)((unpacked32) & 0xFF);
                byte dir = (byte)((unpacked32 >> 8) & 0xFF);
                bool isUOP = (byte)((unpacked32 >> 16) & 0xFF) != 0;

                ref var entry = ref GetAnimationDirectionEntry(animID,
                                                               group,
                                                               dir,
                                                               isUOP);

                if (entry.LastAccessTime != 0)
                {
                    for (int j = 0; j < entry.FramesCount; j++)
                    {
                        _animationCache.Remove(animID, group, dir, j, isUOP);
                    }

                    entry.FramesCount = 0;
                    entry.LastAccessTime = 0;

                    _usedTextures.Remove(first);
                }

                first = next;
            }

            if (_usedTextures.Count != 0)
            {
                _usedTextures.Clear();
            }
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
                ref var entry = ref _animationCache.GetEntry(i);

                bool replace = entry.FileIndex >= 3;

                if (entry.FileIndex == 1)
                {
                    replace = (World.ClientLockedFeatures.Flags & LockedFeatureFlags.LordBlackthornsRevenge) != 0;
                }
                else if (entry.FileIndex == 2)
                {
                    replace = (World.ClientLockedFeatures.Flags & LockedFeatureFlags.AgeOfShadows) != 0;
                }

                if (replace)
                {
                    if (!HasBodyConversion(ref entry))
                    {
                        entry.GraphicConversion = (ushort) (entry.GraphicConversion & ~0x8000);
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
        public void GetSittingAnimDirection(ref byte dir, ref bool mirror, ref int x, ref int y)
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

            ref var entry = ref _animationCache.GetEntry(graphic);

            switch (entry.Type)
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
        public byte GetDieGroupIndex(ushort id, bool second, bool isRunning = false)
        {
            if (id >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                return 0;
            }

            ref var entry = ref _animationCache.GetEntry(id);

            switch (entry.Type)
            {
                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    if ((entry.Flags & ANIMATION_FLAGS.AF_USE_2_IF_HITTED_WHILE_RUNNING) != 0 ||
                        (entry.Flags & ANIMATION_FLAGS.AF_CAN_FLYING) != 0)
                    {
                        return 2;
                    }

                    if ((entry.Flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
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

                    if ((entry.Flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
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
                FixAnimationGraphicAndHue(ref graphic, ref hue, isCorpse, false, false, out bool isUOP);
                ref var dirEntry = ref GetAnimationDirectionEntry(graphic, group, 0, isUOP);

                return dirEntry.Address != IntPtr.Zero && dirEntry.Size != 0;
            }

            return false;
        }

        private bool LoadAnimationFrames(ushort animID, byte animGroup, byte direction, bool isUOP, ref AnimationDirectionEntry animDir)
        {
            if (/*animDir.FileIndex == -1 &&*/ animDir.Address == (IntPtr) (-1))
            {
                return false;
            }

            if (animDir.FileIndex >= _files.Length || animID >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                return false;
            }

            if (animDir.Address == IntPtr.Zero && animDir.Size == 0)
            {
                return false;
            }

            if (isUOP)
            {
                return ReadUOPAnimationFrame(animID, animGroup, direction, ref animDir);
            }

            ReadMULAnimationFrame(animID, animGroup, direction, ref animDir);

            return true;
        }

        private bool ReadUOPAnimationFrame(ushort animID, byte animGroup, byte direction, ref AnimationDirectionEntry animDirection)
        {
            if (animDirection.FileIndex >= _filesUop.Length)
            {
                return false;
            }

            UOFileUop uopFile = _filesUop[animDirection.FileIndex];

            _animationCache.FixUOPGroup(animID, ref animGroup);

            ulong hash = UOFileUop.CreateHash($"build/animationlegacyframe/{animID:D6}/{animGroup:D2}.bin");

            if (!uopFile.TryGetUOPData(hash, out UOFileIndex fileIndex))
            {
                return false;
            }

            if (animDirection.Address == IntPtr.Zero && animDirection.Size == 0 && 
                fileIndex.Length == 0 && fileIndex.DecompressedLength == 0 && fileIndex.Offset == 0)
            {
                Log.Warn("uop animData is null");

                return false;
            }

            animDirection.LastAccessTime = Time.Ticks;

            byte[] buffer = null;
            Span<byte> span = fileIndex.DecompressedLength <= 1024 ? stackalloc byte[fileIndex.DecompressedLength] : (buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(fileIndex.DecompressedLength));

            try
            {
                fixed (byte* ptr = span)
                {
                    ZLib.Decompress
                    (
                        (IntPtr)(animDirection.Address.ToInt64() + fileIndex.Offset),
                        fileIndex.Length,
                        0,
                        (IntPtr)ptr,
                        fileIndex.DecompressedLength
                    );
                }

                StackDataReader reader = new StackDataReader(span);
                reader.Skip(32);

                long end = (long)reader.StartAddress + reader.Length;

                int fc = reader.ReadInt32LE();
                uint dataStart = reader.ReadUInt32LE();
                reader.Seek(dataStart);

                ANIMATION_GROUPS_TYPE type = _animationCache.GetEntry(animID).Type;

                byte framesCount = (byte)(type < ANIMATION_GROUPS_TYPE.EQUIPMENT ? Math.Round(fc / 5f) : 10);

                int headerSize = sizeof(UOPAnimationHeader);
                int count = 0;

                UOPAnimationHeader* animHeaderInfo = (UOPAnimationHeader*)reader.PositionAddress;

                for (ushort i = 0, id = animHeaderInfo->FrameID, currentDir = 0; animHeaderInfo->FrameID < fc; ++i, ++id)
                {
                    if ( /*animHeaderInfo->FrameID != id*/ animHeaderInfo->FrameID - 1 == id || i >= framesCount)
                    {
                        if (currentDir != direction)
                        {
                            ++currentDir;
                        }

                        id = animHeaderInfo->FrameID;
                        i = 0;
                        dataStart = (uint)reader.Position;
                    }
                    else if (animHeaderInfo->FrameID - id > 1)
                    {
                        // error handler?  
                        // reason:
                        //    - anim: 337
                        //    - dir: 3
                        //    - it skips 2 frames )35 --> 38(

                        i += (ushort)(animHeaderInfo->FrameID - id);
                        id = animHeaderInfo->FrameID;
                    }

                    if (i == 0 && currentDir == direction)
                    {
                        break;
                    }

                    reader.Skip(headerSize);

                    animHeaderInfo = (UOPAnimationHeader*)reader.PositionAddress;
                }

                reader.Seek(dataStart);
                animHeaderInfo = (UOPAnimationHeader*)reader.PositionAddress;

                for (ushort id = animHeaderInfo->FrameID; id == animHeaderInfo->FrameID && count < framesCount; ++id, ++count)
                {
                    long start = reader.Position;

                    if (animHeaderInfo->Group == animGroup && start + animHeaderInfo->DataOffset < reader.Length)
                    {
                        int index = animHeaderInfo->FrameID % framesCount;

                        var frame = _animationCache.GetFrame
                        (
                            animID,
                            animGroup,
                            direction,
                            index,
                            true
                        );

                        if (frame == null || frame.IsDisposed)
                        {
                            unchecked
                            {
                                reader.Skip((int)animHeaderInfo->DataOffset);

                                ushort* palette = (ushort*)reader.PositionAddress;
                                reader.Skip(512);

                                uint packed32 = (uint)((animGroup | (direction << 8) | (0x01 << 16)));
                                uint packed32_2 = (uint)((animID | (index << 16)));
                                ulong internalHash = (packed32_2 | ((ulong)packed32 << 32));

                                AnimationFrameTexture texture = ReadFrame(ref reader, end, palette, internalHash);

                                if (texture != null && !texture.IsDisposed)
                                {
                                    _animationCache.Push
                                    (
                                        animID,
                                        animGroup,
                                        direction,
                                        index,
                                        framesCount,
                                        texture,
                                        true
                                    );
                                }

                                reader.Seek(start + headerSize);
                                animHeaderInfo = (UOPAnimationHeader*)reader.PositionAddress;
                            }
                        }
                    }
                }

                animDirection.FramesCount = (byte)count;

                _usedTextures.AddLast((animID | ((ulong)((uint)((animGroup | (direction << 8) | (0x01 << 16)))) << 32)));

                return true;
            }
            finally
            {
                if (buffer != null)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
                               

        private void ReadMULAnimationFrame(ushort animID, byte animGroup, byte direction, ref AnimationDirectionEntry animDir)
        {
            animDir.LastAccessTime = Time.Ticks;
            UOFile file = _files[animDir.FileIndex];

            StackDataReader reader = new StackDataReader(new ReadOnlySpan<byte>((byte*) file.StartAddress, (int) file.Length));
            reader.Seek(animDir.Address.ToInt32());

            long end = (long)reader.StartAddress + reader.Length;

            ushort* palette = (ushort*) reader.PositionAddress;
            reader.Skip(512);

            long dataStart = reader.Position;
            uint frameCount = reader.ReadUInt32LE();
            animDir.FramesCount = (byte) frameCount;
            uint* frameOffset = (uint*) reader.PositionAddress;

            for (int i = 0; i < frameCount; i++)
            {
                reader.Seek(dataStart + frameOffset[i]);

                uint packed32 = (uint)((animGroup | (direction << 8) | (0x00 << 16)));
                uint packed32_2 = (uint)((animID | (i << 16)));
                ulong internalHash = (packed32_2 | ((ulong)packed32 << 32));

                AnimationFrameTexture texture = ReadFrame(ref reader, end, palette, internalHash);

                if (texture != null && !texture.IsDisposed)
                {
                    _animationCache.Push(animID, animGroup, direction, i, (int)frameCount, texture);
                }
            }

            _usedTextures.AddLast
            (
                (ulong)(animID | ((ulong)((uint)((animGroup | (direction << 8) | (0x00 << 16)))) << 32))
            );

            reader.Release();
        }

        private AnimationFrameTexture ReadFrame(ref StackDataReader reader, long end, ushort* palette, ulong hash)
        {
            short imageCenterX = reader.ReadInt16LE();
            short imageCenterY = reader.ReadInt16LE();
            short imageWidth = reader.ReadInt16LE();
            short imageHeight = reader.ReadInt16LE();

            if (imageWidth <= 0 || imageHeight <= 0)
            {
                return null;
            }

            uint[] data = System.Buffers.ArrayPool<uint>.Shared.Rent(imageWidth * imageHeight);

            try
            {
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

                    for (int k = 0; k < runLength; k++)
                    {
                        data[block++] = HuesHelper.Color16To32(palette[reader.ReadUInt8()]) | 0xFF_00_00_00;
                    }

                    header = reader.ReadUInt32LE();
                }

                AnimationFrameTexture f = new AnimationFrameTexture(imageWidth, imageHeight)
                {
                    CenterX = imageCenterX,
                    CenterY = imageCenterY
                };
                
                f.SetData(data, 0, imageWidth * imageHeight);
                
                _picker.Set(hash, imageWidth, imageHeight, data);

                return f;
            }
            finally
            {
                System.Buffers.ArrayPool<uint>.Shared.Return(data, true);
            }
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
            GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
            {
                frameIndex = (byte) animIndex;
            }

            GetAnimationDimensions
            (
                frameIndex,
                graphic,
                dir,
                animGroup,
                out centerX,
                out centerY,
                out width,
                out height
            );

            if (centerX == 0 && centerY == 0 && width == 0 && height == 0)
            {
                height = ismounted ? 100 : 60;
            }
        }

        public void GetAnimationDimensions
        (
            byte frameIndex,
            ushort id,
            byte dir,
            byte animGroup,
            out int x,
            out int y,
            out int w,
            out int h
        )
        {
            if (id < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                if (_animDimensionCache.TryGetValue(id, out Rectangle rect))
                {
                    x = rect.X;
                    y = rect.Y;
                    w = rect.Width;
                    h = rect.Height;

                    return;
                }

                ushort hue = 0;

                if (dir < 5)
                {
                    AnimationFrameTexture frame = GetBodyFrame
                    (
                        ref id,
                        ref hue,
                        animGroup,
                        dir,
                        frameIndex,
                        true,
                        false
                    );


                    if (frame != null)
                    {
                        x = frame.CenterX;
                        y = frame.CenterY;
                        w = frame.Width;
                        h = frame.Height;
                        _animDimensionCache[id] = new Rectangle(x, y, w, h);

                        return;
                    }
                }
            }

            x = 0;
            y = 0;
            w = 0;
            h = 0;
        }

        public void ClearUnusedResources(int maxCount)
        {
            int count = 0;
            long ticks = Time.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            LinkedListNode<ulong> first = _usedTextures.First;

            while (first != null)
            {
                var next = first.Next;

                ulong value = first.Value;

                ushort animID = (ushort) (0xFFFF_FFFF & value);
                uint unpacked32 = (uint) (value >> 32);

                byte group = (byte)((unpacked32) & 0xFF);
                byte dir = (byte)((unpacked32 >> 8) & 0xFF);
                bool isUOP = (byte)((unpacked32 >> 16) & 0xFF) != 0;

                ref var entry = ref GetAnimationDirectionEntry(animID,
                                                               group,
                                                               dir,
                                                               isUOP);

                if (entry.LastAccessTime != 0 && entry.LastAccessTime < ticks)
                {
                    for (int j = 0; j < entry.FramesCount; j++)
                    {
                        _animationCache.Remove(animID, group, dir, j, isUOP);
                    }

                    entry.FramesCount = 0;
                    entry.LastAccessTime = 0;

                    _usedTextures.Remove(first);

                    if (++count >= maxCount)
                    {
                        break;
                    }
                }

                first = next;
            }
        }

        private long CalculateOffset(ushort graphic, ANIMATION_FLAGS flags, ANIMATION_GROUPS_TYPE type, out int groupCount)
        {
            long result = 0;
            groupCount = 0;

            ANIMATION_GROUPS group = ANIMATION_GROUPS.AG_NONE;

            switch (type)
            {
                case ANIMATION_GROUPS_TYPE.MONSTER:

                    if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP) != 0)
                    {
                        group = ANIMATION_GROUPS.AG_PEOPLE;
                    }
                    else if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
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
                    groupCount = (int)LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;

                    break;

                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_LOW_GROUP_EXTENDED) != 0)
                    {
                        if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP) != 0)
                        {
                            group = ANIMATION_GROUPS.AG_PEOPLE;
                        }
                        else if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
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
                    result = CalculateLowGroupOffset(graphic);
                    groupCount = (int)LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;

                    break;

                case ANIMATION_GROUPS.AG_HIGHT:
                    result = CalculateHighGroupOffset(graphic);
                    groupCount = (int)HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT;

                    break;

                case ANIMATION_GROUPS.AG_PEOPLE:
                    result = CalculatePeopleGroupOffset(graphic);
                    groupCount = (int)PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;

                    break;
            }

            return result;
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

    internal enum ANIMATION_GROUPS : byte
    {
        AG_NONE = 0,
        AG_LOW,
        AG_HIGHT,
        AG_PEOPLE
    }

    internal enum ANIMATION_GROUPS_TYPE : byte
    {
        UNKNOWN = 0,
        MONSTER,
        SEA_MONSTER,
        ANIMAL,
        HUMAN,
        EQUIPMENT,
    }

    internal enum HIGHT_ANIMATION_GROUP : byte
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

    internal enum PEOPLE_ANIMATION_GROUP : byte
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

    internal enum LOW_ANIMATION_GROUP : byte
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

    struct AnimationEntry
    {
        public ANIMATION_FLAGS Flags;
        public ushort Graphic;
        public ushort GraphicConversion;
        public ushort CorpseGraphic;
        public ushort Color;
        public ushort CorpseColor;
        public ANIMATION_GROUPS_TYPE Type;
        public byte FileIndex;
        public sbyte MountOffsetY;
        public bool IsValidMUL;

        public bool IsValid => (Flags & ANIMATION_FLAGS.AF_FOUND) != 0;
        public bool IsUOP => (Flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0;

        public bool IsMixed => IsUOP && IsValidMUL;
    }

    struct AnimationDirectionEntry
    {
        public IntPtr Address;
        public uint Size;
        public uint LastAccessTime;
        public byte FramesCount;
        public byte FileIndex;
    }


    class AnimationFrameSequenceTextureAtlas
    {
        private Point _offset;
        private Point _maxSize;
        private Texture2D _atlas;

        public AnimationFrameSequenceTextureAtlas(int width, int height)
        {
            _atlas = new Texture2D(Client.Game.GraphicsDevice, width, height);
        }

        public void AddFrame(uint[] buffer, short centerX, short centerY, int width, int height)
        {
            if (_offset.X + width > _atlas.Width)
            {
                _offset.Y += height;
            }

            Rectangle rect = new Rectangle(_offset.X, _offset.Y, width, height);

            _atlas.SetData(0, rect, buffer, 0, buffer.Length);

            _offset.X += width;
            _offset.Y += height;
        }

    }

    class AnimationCache
    {
        private readonly bool _uopSupport;

        private readonly AnimationEntry[] _indexEntries = new AnimationEntry[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT];
        private readonly AnimationFrameTexture[,,][] _cache;
        private readonly AnimationDirectionEntry[,,] _indexCache;
        private readonly byte[,] _uopIndexConvertionCache;

        public AnimationCache(bool enableUOPSupport)
        {
            _uopSupport = enableUOPSupport;

            int multi = enableUOPSupport ? 2 : 1;

            _cache = new AnimationFrameTexture[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT, 100 * multi /* uop */, 5][];
            _indexCache = new AnimationDirectionEntry[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT, 100 * (multi + 1) /* uop + bodyConv */, 5];

            if (enableUOPSupport)
            {
                _uopIndexConvertionCache = new byte[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT, 100];
            }
        }

        public ref AnimationEntry this[int graphic] => ref _indexEntries[graphic];


        public ref AnimationEntry GetEntry(int graphic)
        {
            ref AnimationEntry entry = ref _indexEntries[graphic];

            return ref entry;
        }
    
        public ref AnimationDirectionEntry GetDirectionEntry(ushort animID, byte group, byte direction, bool uop = false, bool converted = false)
        {
            if (uop && _uopSupport)
            {
                group = _uopIndexConvertionCache[animID, group];
                group += 100;
            }

            if (converted)
            {
                group += 100;
            }

            ref AnimationDirectionEntry entry = ref _indexCache[animID, group, direction];

            return ref entry;
        }

        
        public bool Push(ushort animID, byte group, byte direction, int frame, int totalFrames, AnimationFrameTexture texture, bool uop = false)
        {            
            if (frame >= totalFrames || totalFrames <= 0)
            {
                return false;
            }

            if (uop && _uopSupport)
            {
                group = _uopIndexConvertionCache[animID, group];
                group += 100;
            }

            ref var frames = ref _cache[animID, group, direction];

            if (frames == null)
            {
                frames = new AnimationFrameTexture[totalFrames];
            }

            if (frame < frames.Length)
            {
                if (frames[frame] != null)
                {
                    Log.Error($"animation 0x{animID:X4} already exists");

                    return false;
                }
            }

            frames[frame] = texture;

            return true;
        }

        public bool Remove(ushort animID, byte group, byte direction, int frame, bool uop = false)
        {
            if (uop && _uopSupport)
            {
                group = _uopIndexConvertionCache[animID, group];
                group += 100;
            }

            AnimationFrameTexture[] frames = _cache[animID, group, direction];

            if (frames != null && frame < frames.Length)
            {
                ref AnimationFrameTexture texture = ref frames[frame];

                if (texture != null)
                {
                    texture.Dispose();
                    texture = null;

                    return true;
                }
            }

            return false;
        }

        public void ReplaceUopGroup(ushort animID, byte oldGroup, byte newGroup)
        {
            if (_uopSupport)
            {
                _uopIndexConvertionCache[animID, oldGroup] = newGroup;
            }
        }

        public void FixUOPGroup(ushort animID, ref byte group)
        {
            if (_uopSupport)
            {
                group = _uopIndexConvertionCache[animID, group];
            }
        }

        public AnimationFrameTexture GetFrame(ushort animID, byte group, byte direction, int frame, bool uop = false)
        {
            if (uop && _uopSupport)
            {
                group = _uopIndexConvertionCache[animID, group];
                group += 100;
            }

            AnimationFrameTexture[] frames = _cache[animID, group, direction];

            if (frames != null)
            {
                if (frame >= frames.Length)
                {
                    frame = frames.Length - 1;
                }

                if (frame < 0)
                {
                    frame = 0;
                }

                return _cache[animID, group, direction][frame];
            }

            return null;
        }
    }

    internal readonly struct EquipConvData : IEquatable<EquipConvData>
    {
        public EquipConvData(ushort graphic, ushort gump, ushort color)
        {
            Graphic = graphic;
            Gump = gump;
            Color = color;
        }

        public readonly ushort Graphic;
        public readonly ushort Gump;
        public readonly ushort Color;


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