#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.IO.Resources
{
    internal class AnimationsLoader : ResourceLoader<AnimationFrameTexture>
    {
        private readonly Dictionary<ushort, byte> _animationSequenceReplacing = new Dictionary<ushort, byte>();
        private readonly Dictionary<Graphic, Rectangle> _animDimensionCache = new Dictionary<Graphic, Rectangle>();
        private readonly AnimationGroup _empty = new AnimationGroup
        {
            Direction = new AnimationDirection[5]
            {
                new AnimationDirection(){ FileIndex = -1, Address = -1},
                new AnimationDirection(){ FileIndex = -1, Address = -1},
                new AnimationDirection(){ FileIndex = -1, Address = -1},
                new AnimationDirection(){ FileIndex = -1, Address = -1},
                new AnimationDirection(){ FileIndex = -1, Address = -1}
            }
        };
        private readonly Dictionary<ushort, Dictionary<ushort, EquipConvData>> _equipConv = new Dictionary<ushort, Dictionary<ushort, EquipConvData>>();
        private readonly UOFileMul[] _files = new UOFileMul[5];
        private readonly UOFileUop[] _filesUop = new UOFileUop[4];
        //private readonly List<ToRemoveInfo> _usedTextures = new List<ToRemoveInfo>(), _usedUopTextures = new List<ToRemoveInfo>();

        private readonly List<AnimationDirection> _usedTextures = new List<AnimationDirection>();


        public ushort Color { get; set; }
        public byte AnimGroup { get; set; }
        public byte Direction { get; set; }
        public ushort AnimID { get; set; }
        public int SittingValue { get; set; }

        public IndexAnimation[] DataIndex { get; } = new IndexAnimation[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT];

        public IReadOnlyDictionary<ushort, Dictionary<ushort, EquipConvData>> EquipConversions => _equipConv;

        public List<Tuple<ushort, byte>>[] GroupReplaces { get; } = new List<Tuple<ushort, byte>>[2]
        {
            new List<Tuple<ushort, byte>>(), new List<Tuple<ushort, byte>>()
        };

        public SittingInfoData[] SittingInfos { get; } =
        {
            new SittingInfoData(0x0459, 0, -1, 4, -1, 2, 2, false),
            new SittingInfoData(0x045A, -1, 2, -1, 6, 2, 2, false),
            new SittingInfoData(0x045B, 0, -1, 4, -1, 2, 2, false),
            new SittingInfoData(0x045C, -1, 2, -1, 6, 2, 2, false),
            new SittingInfoData(0x0A2A, 0, 2, 4, 6, -4, -4, false),
            new SittingInfoData(0x0A2B, 0, 2, 4, 6, -8, -8, false),
            new SittingInfoData(0x0B2C, -1, 2, -1, 6, 2, 2, false),
            new SittingInfoData(0x0B2D, 0, -1, 4, -1, 2, 2, false),
            new SittingInfoData(0x0B2E, 4, 4, 4, 4, 0, 0, false),
            new SittingInfoData(0x0B2F, 2, 2, 2, 2, 6, 6, false),
            new SittingInfoData(0x0B30, 6, 6, 6, 6, -8, 8, true),
            new SittingInfoData(0x0B31, 0, 0, 0, 0, 0, 4, true),
            new SittingInfoData(0x0B32, 4, 4, 4, 4, 0, 0, false),
            new SittingInfoData(0x0B33, 2, 2, 2, 2, 0, 0, false),
            new SittingInfoData(0x0B4E, 2, 2, 2, 2, 0, 0, false),
            new SittingInfoData(0x0B4F, 4, 4, 4, 4, 0, 0, false),
            new SittingInfoData(0x0B50, 0, 0, 0, 0, 0, 0, true),
            new SittingInfoData(0x0B51, 6, 6, 6, 6, 0, 0, true),
            new SittingInfoData(0x0B52, 2, 2, 2, 2, 0, 0, false),
            new SittingInfoData(0x0B53, 4, 4, 4, 4, 0, 0, false),
            new SittingInfoData(0x0B54, 0, 0, 0, 0, 0, 0, true),
            new SittingInfoData(0x0B55, 6, 6, 6, 6, 0, 0, true),
            new SittingInfoData(0x0B56, 2, 2, 2, 2, 4, 4, false),
            new SittingInfoData(0x0B57, 4, 4, 4, 4, 4, 4, false),
            new SittingInfoData(0x0B58, 6, 6, 6, 6, 0, 8, true),
            new SittingInfoData(0x0B59, 0, 0, 0, 0, 0, 8, true),
            new SittingInfoData(0x0B5A, 2, 2, 2, 2, 8, 8, false),
            new SittingInfoData(0x0B5B, 4, 4, 4, 4, 8, 8, false),
            new SittingInfoData(0x0B5C, 0, 0, 0, 0, 0, 8, true),
            new SittingInfoData(0x0B5D, 6, 6, 6, 6, 0, 8, true),
            new SittingInfoData(0x0B5E, 0, 2, 4, 6, -8, -8, false),
            new SittingInfoData(0x0B5F, -1, 2, -1, 6, 3, 14, false),
            new SittingInfoData(0x0B60, -1, 2, -1, 6, 3, 14, false),
            new SittingInfoData(0x0B61, -1, 2, -1, 6, 3, 14, false),
            new SittingInfoData(0x0B62, -1, 2, -1, 6, 3, 10, false),
            new SittingInfoData(0x0B63, -1, 2, -1, 6, 3, 10, false),
            new SittingInfoData(0x0B64, -1, 2, -1, 6, 3, 10, false),
            new SittingInfoData(0x0B65, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x0B66, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x0B67, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x0B68, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x0B69, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x0B6A, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x0B91, 4, 4, 4, 4, 6, 6, false),
            new SittingInfoData(0x0B92, 4, 4, 4, 4, 6, 6, false),
            new SittingInfoData(0x0B93, 2, 2, 2, 2, 6, 6, false),
            new SittingInfoData(0x0B94, 2, 2, 2, 2, 6, 6, false),
            new SittingInfoData(0x0CF3, -1, 2, -1, 6, 2, 8, false),
            new SittingInfoData(0x0CF4, -1, 2, -1, 6, 2, 8, false),
            new SittingInfoData(0x0CF6, 0, -1, 4, -1, 2, 8, false),
            new SittingInfoData(0x0CF7, 0, -1, 4, -1, 2, 8, false),
            new SittingInfoData(0x11FC, 0, 2, 4, 6, 2, 7, false),
            new SittingInfoData(0x1218, 4, 4, 4, 4, 4, 4, false),
            new SittingInfoData(0x1219, 2, 2, 2, 2, 4, 4, false),
            new SittingInfoData(0x121A, 0, 0, 0, 0, 0, 8, true),
            new SittingInfoData(0x121B, 6, 6, 6, 6, 0, 8, true),
            new SittingInfoData(0x1527, 2, 2, 2, 2, 0, 0, false),
            new SittingInfoData(0x1771, 0, 2, 4, 6, 0, 0, false),
            new SittingInfoData(0x1776, 0, 2, 4, 6, 0, 0, false),
            new SittingInfoData(0x1779, 0, 2, 4, 6, 0, 0, false),
            new SittingInfoData(0x1DC7, -1, 2, -1, 6, 3, 10, false),
            new SittingInfoData(0x1DC8, -1, 2, -1, 6, 3, 10, false),
            new SittingInfoData(0x1DC9, -1, 2, -1, 6, 3, 10, false),
            new SittingInfoData(0x1DCA, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x1DCB, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x1DCC, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x1DCD, -1, 2, -1, 6, 3, 10, false),
            new SittingInfoData(0x1DCE, -1, 2, -1, 6, 3, 10, false),
            new SittingInfoData(0x1DCF, -1, 2, -1, 6, 3, 10, false),
            new SittingInfoData(0x1DD0, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x1DD1, 0, -1, 4, -1, 3, 10, false),
            new SittingInfoData(0x1DD2, -1, 2, -1, 6, 3, 10, false),

            new SittingInfoData(0x2A58, 4, 4, 4, 4, 0, 0, false),
            new SittingInfoData(0x2A59, 2, 2, 2, 2, 0, 0, false),
            new SittingInfoData(0x2A5A, 0, 2, 4, 6, 0, 0, false),
            new SittingInfoData(0x2A5B, 0, 2, 4, 6, 10, 10, false),
            new SittingInfoData(0x2A7F, 0, 2, 4, 6, 0, 0, false),
            new SittingInfoData(0x2A80, 0, 2, 4, 6, 0, 0, false),
            new SittingInfoData(0x2DDF, 0, 2, 4, 6, 2, 2, false),
            new SittingInfoData(0x2DE0, 0, 2, 4, 6, 2, 2, false),
            new SittingInfoData(0x2DE3, 2, 2, 2, 2, 4, 4, false),
            new SittingInfoData(0x2DE4, 4, 4, 4, 4, 4, 4, false),
            new SittingInfoData(0x2DE5, 6, 6, 6, 6, 4, 4, false),
            new SittingInfoData(0x2DE6, 0, 0, 0, 0, 4, 4, false),
            new SittingInfoData(0x2DEB, 0, 0, 0, 0, 4, 4, false),
            new SittingInfoData(0x2DEC, 4, 4, 4, 4, 4, 4, false),
            new SittingInfoData(0x2DED, 2, 2, 2, 2, 4, 4, false),
            new SittingInfoData(0x2DEE, 6, 6, 6, 6, 4, 4, false),
            new SittingInfoData(0x2DF5, 0, 2, 4, 6, 4, 4, false),
            new SittingInfoData(0x2DF6, 0, 2, 4, 6, 4, 4, false),
            new SittingInfoData(0x3088, 0, 2, 4, 6, 4, 4, false),
            new SittingInfoData(0x3089, 0, 2, 4, 6, 4, 4, false),
            new SittingInfoData(0x308A, 0, 2, 4, 6, 4, 4, false),
            new SittingInfoData(0x308B, 0, 2, 4, 6, 4, 4, false),
            new SittingInfoData(0x35ED, 0, 2, 4, 6, 0, 0, false),
            new SittingInfoData(0x35EE, 0, 2, 4, 6, 0, 0, false),

            new SittingInfoData(0x3DFF, 0, -1, 4, -1, 2, 2, false),
            new SittingInfoData(0x3E00, -1, 2, -1, 6, 2, 2, false)
        };


        public override Task Load()
        {
            return Task.Run(() =>
            {

                bool loaduop = false;
                int[] un = { 0x40000, 0x10000, 0x20000, 0x20000, 0x20000 };

                for (int i = 0; i < 5; i++)
                {
                    string pathmul = Path.Combine(FileManager.UoFolderPath, "anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".mul");
                    string pathidx = Path.Combine(FileManager.UoFolderPath, "anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".idx");

                    if (File.Exists(pathmul) && File.Exists(pathidx))
                    {
                        _files[i] = new UOFileMul(pathmul, pathidx, un[i], i == 0 ? 6 : -1);
                    }

                    if (i > 0 && FileManager.ClientVersion >= ClientVersions.CV_7000)
                    {
                        string pathuop = Path.Combine(FileManager.UoFolderPath, $"AnimationFrame{i}.uop");

                        if (File.Exists(pathuop))
                        {
                            _filesUop[i - 1] = new UOFileUop(pathuop, "build/animationlegacyframe/{0:D6}/{0:D2}.bin");

                            if (!loaduop)
                                loaduop = true;
                        }
                    }
                }

                if (loaduop)
                    LoadUop();

                int animIdxBlockSize = UnsafeMemoryManager.SizeOf<AnimIdxBlock>();
                UOFile idxfile0 = _files[0]?.IdxFile;
                long? maxAddress0 = (long?)idxfile0?.StartAddress + idxfile0?.Length;
                UOFile idxfile2 = _files[1]?.IdxFile;
                long? maxAddress2 = (long?)idxfile2?.StartAddress + idxfile2?.Length;
                UOFile idxfile3 = _files[2]?.IdxFile;
                long? maxAddress3 = (long?)idxfile3?.StartAddress + idxfile3?.Length;
                UOFile idxfile4 = _files[3]?.IdxFile;
                long? maxAddress4 = (long?)idxfile4?.StartAddress + idxfile4?.Length;
                UOFile idxfile5 = _files[4]?.IdxFile;
                long? maxAddress5 = (long?)idxfile5?.StartAddress + idxfile5?.Length;

                if (FileManager.ClientVersion >= ClientVersions.CV_500A)
                {
                    string path = Path.Combine(FileManager.UoFolderPath, "mobtypes.txt");

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

                                if (line.Length == 0 || line[0] == '#')
                                    continue;

                                string[] parts = line.Split(new[]
                                {
                                '\t', ' '
                            }, StringSplitOptions.RemoveEmptyEntries);

                                if (parts.Length < 3)
                                    continue;

                                int id = int.Parse(parts[0]);

                                if (id >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                    continue;

                                string testType = parts[1].ToLower();
                                int commentIdx = parts[2].IndexOf('#');

                                if (commentIdx > 0)
                                    parts[2] = parts[2].Substring(0, commentIdx - 1);
                                else if (commentIdx == 0)
                                    continue;

                                uint number = uint.Parse(parts[2], NumberStyles.HexNumber);


                                for (int i = 0; i < 5; i++)
                                {
                                    if (testType == typeNames[i])
                                    {
                                        ref IndexAnimation index = ref DataIndex[id];

                                        if (index == null)
                                            index = new IndexAnimation();

                                        index.Type = (ANIMATION_GROUPS_TYPE)i;
                                        index.Flags = 0x80000000 | number;

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                for (ushort i = 0; i < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; i++)
                {
                    if (DataIndex[i] == null)
                        DataIndex[i] = new IndexAnimation();

                    if (DataIndex[i].Type == ANIMATION_GROUPS_TYPE.UNKNOWN)
                        DataIndex[i].Type = CalculateTypeByGraphic(i);

                    DataIndex[i].Graphic = i;
                    DataIndex[i].CorpseGraphic = i;

                    long offsetToData = DataIndex[i].CalculateOffset(i, out int count);

                    if (offsetToData >= idxfile0.Length) continue;

                    bool isValid = false;

                    long address = _files[0].IdxFile.StartAddress.ToInt64() + offsetToData;
                    DataIndex[i].Groups = new AnimationGroup[100];

                    int offset = 0;

                    for (byte j = 0; j < 100; j++)
                    {
                        DataIndex[i].Groups[j] = new AnimationGroup
                        {
                            Direction = new AnimationDirection[5]
                        };

                        if (j >= count)
                            continue;

                        for (byte d = 0; d < 5; d++)
                        {
                            if (DataIndex[i].Groups[j].Direction[d] == null)
                                DataIndex[i].Groups[j].Direction[d] = new AnimationDirection();

                            unsafe
                            {
                                AnimIdxBlock* aidx = (AnimIdxBlock*)(address + offset * animIdxBlockSize);
                                offset++;

                                if ((long)aidx >= maxAddress0)
                                    break;

                                if (aidx->Size != 0 && aidx->Position != 0xFFFFFFFF && aidx->Size != 0xFFFFFFFF)
                                {
                                    DataIndex[i].Groups[j].Direction[d].Address = aidx->Position;
                                    DataIndex[i].Groups[j].Direction[d].Size = aidx->Size;

                                    isValid = true;
                                }
                            }
                        }
                    }

                    DataIndex[i].IsValidMUL = isValid;
                }

                string file = Path.Combine(FileManager.UoFolderPath, "Anim1.def");

                if (File.Exists(file))
                {
                    using (DefReader defReader = new DefReader(file))
                    {
                        while (defReader.Next())
                        {
                            ushort group = (ushort)defReader.ReadInt();
                            int replace = defReader.ReadGroupInt();
                            GroupReplaces[0].Add(new Tuple<ushort, byte>(group, (byte)replace));
                        }
                    }
                }

                file = Path.Combine(FileManager.UoFolderPath, "Anim2.def");

                if (File.Exists(file))
                {
                    using (DefReader defReader = new DefReader(file))
                    {
                        while (defReader.Next())
                        {
                            ushort group = (ushort)defReader.ReadInt();
                            int replace = defReader.ReadGroupInt();
                            GroupReplaces[1].Add(new Tuple<ushort, byte>(group, (byte)replace));
                        }
                    }
                }

                if (FileManager.ClientVersion < ClientVersions.CV_305D)
                    return;

                file = Path.Combine(FileManager.UoFolderPath, "Equipconv.def");

                if (File.Exists(file))
                {
                    using (DefReader defReader = new DefReader(file, 5))
                    {
                        while (defReader.Next())
                        {
                            ushort body = (ushort)defReader.ReadInt();

                            if (body >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                continue;

                            ushort graphic = (ushort)defReader.ReadInt();

                            if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                continue;

                            ushort newGraphic = (ushort)defReader.ReadInt();

                            if (newGraphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                continue;

                            int gump = defReader.ReadInt();

                            if (gump > ushort.MaxValue)
                                continue;

                            if (gump == 0)
                                gump = graphic;
                            else if (gump == 0xFFFF)
                                gump = newGraphic;

                            ushort color = (ushort)defReader.ReadInt();

                            if (!_equipConv.TryGetValue(body, out Dictionary<ushort, EquipConvData> dict))
                            {
                                _equipConv.Add(body, new Dictionary<ushort, EquipConvData>());

                                if (!_equipConv.TryGetValue(body, out dict))
                                    continue;
                            }

                            dict[graphic] = new EquipConvData(newGraphic, (ushort)gump, color);
                        }
                    }
                }

                file = Path.Combine(FileManager.UoFolderPath, "Bodyconv.def");

                if (File.Exists(file))
                {
                    using (DefReader defReader = new DefReader(file))
                    {
                        while (defReader.Next())
                        {
                            ushort index = (ushort)defReader.ReadInt();

                            if (index >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                continue;

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

                                    if (defReader.PartsCount >= 5) anim[3] = defReader.ReadInt();
                                }
                            }

                            int animFile = 0;
                            ushort realAnimID = 0xFFFF;
                            sbyte mountedHeightOffset = 0;

                            if (anim[0] != -1 && maxAddress2.HasValue && maxAddress2 != 0)
                            {
                                animFile = 1;
                                realAnimID = (ushort)anim[0];

                                if (index == 0x00C0 || index == 793)
                                    mountedHeightOffset = -9;
                            }
                            else if (anim[1] != -1 && maxAddress3.HasValue && maxAddress3 != 0)
                            {
                                animFile = 2;
                                realAnimID = (ushort)anim[1];
                            }
                            else if (anim[2] != -1 && maxAddress4.HasValue && maxAddress4 != 0)
                            {
                                animFile = 3;
                                realAnimID = (ushort)anim[2];
                            }
                            else if (anim[3] != -1 && maxAddress5.HasValue && maxAddress5 != 0)
                            {
                                animFile = 4;
                                realAnimID = (ushort)anim[3];
                                mountedHeightOffset = -9;

                                if (index == 0x0115 || index == 0x00C0)
                                    mountedHeightOffset = 0;
                            }


                            if (realAnimID != 0xFFFF && animFile != 0)
                            {
                                UOFile currentIdxFile = _files[animFile].IdxFile;
                                long addressOffset = DataIndex[index].CalculateOffset(realAnimID, out int count);

                                if (addressOffset < currentIdxFile.Length)
                                {
                                    DataIndex[index].MountedHeightOffset = mountedHeightOffset;
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
                                            DataIndex[index].BodyConvGroups[j].Direction = new AnimationDirection[5];

                                        for (byte d = 0; d < 5; d++)
                                        {
                                            if (DataIndex[index].BodyConvGroups[j].Direction[d] == null)
                                                DataIndex[index].BodyConvGroups[j].Direction[d] = new AnimationDirection();

                                            unsafe
                                            {
                                                AnimIdxBlock* aidx = (AnimIdxBlock*)(addressOffset + offset * animIdxBlockSize);
                                                offset++;

                                                if ((long)aidx >= maxaddress) continue;

                                                if (aidx->Size != 0 && aidx->Position != 0xFFFFFFFF && aidx->Size != 0xFFFFFFFF)
                                                {
                                                    ref var dataindex = ref DataIndex[index].BodyConvGroups[j].Direction[d];

                                                    dataindex.Address = aidx->Position;
                                                    dataindex.Size = aidx->Size;
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

                file = Path.Combine(FileManager.UoFolderPath, "Body.def");

                if (File.Exists(file))
                {
                    using (DefReader defReader = new DefReader(file, 1))
                    {
                        while (defReader.Next())
                        {
                            int index = defReader.ReadInt();

                            if (index >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                continue;

                            int[] group = defReader.ReadGroup();
                            int color = defReader.ReadInt();

                            for (int i = 0; i < group.Length; i++)
                            {
                                int checkIndex = group[i];

                                if (checkIndex >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                    continue;

                                DataIndex[index].Graphic = (ushort)checkIndex;
                                DataIndex[index].Color = (ushort)color;
                                DataIndex[index].IsValidMUL = true;

                                break;
                            }
                        }
                    }
                }

                file = Path.Combine(FileManager.UoFolderPath, "Corpse.def");

                if (File.Exists(file))
                {
                    using (DefReader defReader = new DefReader(file, 1))
                    {
                        while (defReader.Next())
                        {
                            ushort index = (ushort)defReader.ReadInt();

                            if (index >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                continue;

                            int[] group = defReader.ReadGroup();

                            ushort color = (ushort)defReader.ReadInt();

                            for (int i = 0; i < group.Length; i++)
                            {
                                int checkIndex = group[i];

                                if (checkIndex >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                                    continue;

                                DataIndex[index].CorpseGraphic = (ushort)checkIndex;
                                DataIndex[index].CorpseColor = color;
                                DataIndex[index].IsValidMUL = true;

                                break;
                            }
                        }
                    }
                }
            });
        }

        private void LoadUop()
        {
            if (FileManager.ClientVersion <= ClientVersions.CV_60144)
                return;

            for (ushort animID = 0; animID < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; animID++)
            {
                for (byte grpID = 0; grpID < 100; grpID++)
                {
                    string hashstring = $"build/animationlegacyframe/{animID:D6}/{grpID:D2}.bin";
                    ulong hash = UOFileUop.CreateHash(hashstring);

                    for (int i = 0; i < _filesUop.Length; i++)
                    {
                        var uopFile = _filesUop[i];
                        if (uopFile != null && uopFile.TryGetUOPData(hash, out var data))
                        {
                            if (DataIndex[animID] == null)
                            {
                                DataIndex[animID] = new IndexAnimation
                                {
                                    UopGroups = new AnimationGroupUop[100]
                                };

                                DataIndex[animID].InitializeUOP();
                            }

                            ref var g = ref DataIndex[animID].UopGroups[grpID];

                            g = new AnimationGroupUop
                            {
                                Offset = (uint)data.Offset,
                                CompressedLength = (uint)data.Length,
                                DecompressedLength = (uint)data.DecompressedLength,
                                FileIndex = i,
                                Direction = new AnimationDirection[5]
                            };

                            for (int d = 0; d < 5; d++)
                            {
                                if (g.Direction[d] == null)
                                    g.Direction[d] = new AnimationDirection();
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

            string animationSequencePath = Path.Combine(FileManager.UoFolderPath, "AnimationSequence.uop");

            if (!File.Exists(animationSequencePath))
            {
                Log.Message(LogTypes.Warning, "AnimationSequence.uop not found");

                return;
            }

            UOFileUop animSeq = new UOFileUop(animationSequencePath, "build/animationsequence/{0:D8}.bin");
            UOFileIndex[] animseqEntries = new UOFileIndex[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT];
            animSeq.FillEntries(ref animseqEntries);
            DataReader reader = new DataReader();

            for (int i = 0; i < animseqEntries.Length; i++)
            {
                ref readonly UOFileIndex entry = ref animseqEntries[i];

                if (entry.Offset == 0)
                    continue;

                animSeq.Seek(entry.Offset);

                byte[] decbuffer = animSeq.GetData(entry.Length, entry.DecompressedLength);

                reader.SetData(decbuffer, decbuffer.Length);
                uint animID = reader.ReadUInt();
                reader.Skip(48);
                int replaces = reader.ReadInt();


                //for (byte grpID = 0; grpID < 100; grpID++)
                //{
                //    string hashstring = $"build/animationlegacyframe/{animID:D6}/{grpID:D2}.bin";
                //    ulong hash = UOFileUop.CreateHash(hashstring);

                //    if (hashes.TryGetValue(hash, out var uopData))
                //    {
                //        if (DataIndex[animID] == null)
                //            DataIndex[animID] = new IndexAnimation()
                //            {
                //                UopGroups = new AnimationGroupUop[100]
                //            };


                //        if (DataIndex[animID].Type == ANIMATION_GROUPS_TYPE.UNKNOWN)
                //        {
                //            switch (replaces)
                //            {
                //                case 29:
                //                    DataIndex[animID].Type = ANIMATION_GROUPS_TYPE.MONSTER;

                //                    break;
                //                case 31:
                //                case 32:
                //                    DataIndex[animID].Type = ANIMATION_GROUPS_TYPE.ANIMAL;

                //                    break;
                //                case 48:
                //                case 68:
                //                    DataIndex[animID].Type = ANIMATION_GROUPS_TYPE.HUMAN;

                //                    break;
                //            }
                //        }


                //        //ref var g = ref DataIndex[animID].UopGroups[grpID];

                //        //g = new AnimationGroupUop
                //        //{
                //        //    Offset = uopData.Offset,
                //        //    CompressedLength = uopData.CompressedLength,
                //        //    DecompressedLength = uopData.DecompressedLength,
                //        //    FileIndex = uopData.FileIndex,
                //        //    Direction = new AnimationDirection[5]
                //        //};


                //        //for (int d = 0; d < 5; d++)
                //        //{
                //        //    g.Direction[d].IsUOP = true;
                //        //}
                //    }
                //}


                if (replaces == 48 || replaces == 68)
                    continue;

                for (int k = 0; k < replaces; k++)
                {
                    int oldGroup = reader.ReadInt();
                    uint frameCount = reader.ReadUInt();
                    int newGroup = reader.ReadInt();

                    if (frameCount == 0 && DataIndex[animID] != null)
                        DataIndex[animID].ReplaceUopGroup((byte)oldGroup, (byte)newGroup);

                    reader.Skip(60);
                }
            }

            animSeq.Dispose();
            reader.ReleaseData();
        }



        public static uint CalculatePeopleGroupOffset(ushort graphic)
        {
            return (uint)(((graphic - 400) * 175 + 35000) * UnsafeMemoryManager.SizeOf<AnimIdxBlock>());
        }

        public static uint CalculateHighGroupOffset(ushort graphic)
        {
            return (uint)(graphic * 110 * UnsafeMemoryManager.SizeOf<AnimIdxBlock>());
        }

        public static uint CalculateLowGroupOffset(ushort graphic)
        {
            return (uint)(((graphic - 200) * 65 + 22000) * UnsafeMemoryManager.SizeOf<AnimIdxBlock>());
        }

        private ANIMATION_GROUPS_TYPE CalculateTypeByGraphic(ushort graphic)
        {
            return graphic < 200 ? ANIMATION_GROUPS_TYPE.MONSTER : graphic < 400 ? ANIMATION_GROUPS_TYPE.ANIMAL : ANIMATION_GROUPS_TYPE.HUMAN;
        }


        public AnimationGroup GetBodyAnimationGroup(ref ushort graphic, ref byte group, ref ushort hue, bool isParent = false)
        {
            if (graphic < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && group < 100)
            {
                ref var index = ref DataIndex[graphic];

                if (index.IsUOP && (isParent || !index.IsValidMUL))
                {
                    var uop = index.GetUopGroup(group);

                    return uop ?? _empty;
                }

                ushort newGraphic = index.Graphic;

                do
                {
                    if ((DataIndex[newGraphic].HasBodyConversion || !index.HasBodyConversion) && !(DataIndex[newGraphic].HasBodyConversion && index.HasBodyConversion))
                    {
                        if (graphic != newGraphic)
                        {
                            graphic = newGraphic;
                            hue = index.Color;
                            newGraphic = DataIndex[graphic].Graphic;
                        }
                    }
                    else
                        break;
                } while (graphic != newGraphic);


                if (DataIndex[graphic].HasBodyConversion && DataIndex[graphic].BodyConvGroups != null)
                {
                    return DataIndex[graphic].BodyConvGroups[group] ?? _empty;
                }

                if (DataIndex[graphic].Groups != null && DataIndex[graphic].Groups[group] != null)
                {
                    return DataIndex[graphic].Groups[group];
                }
            }

            return _empty;
        }

        public AnimationGroup GetCorpseAnimationGroup(ref ushort graphic, ref byte group, ref ushort hue)
        {
            if (graphic < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && group < 100)
            {
                ref var index = ref DataIndex[graphic];

                if (index.IsUOP)
                {
                    var uop = index.GetUopGroup(group);

                    return uop ?? _empty;
                }

                ushort newGraphic = index.CorpseGraphic;

                do
                {
                    if ((DataIndex[newGraphic].HasBodyConversion || !index.HasBodyConversion) && !(DataIndex[newGraphic].HasBodyConversion && index.HasBodyConversion))
                    {
                        if (graphic != newGraphic)
                        {
                            graphic = newGraphic;
                            hue = index.CorpseColor;
                            newGraphic = DataIndex[graphic].CorpseGraphic;
                        }
                    }
                    else
                        break;
                } while (graphic != newGraphic);

                if (DataIndex[graphic].HasBodyConversion)
                    return DataIndex[graphic].BodyConvGroups != null ? DataIndex[graphic].BodyConvGroups[group] : _empty;

                return DataIndex[graphic].Groups != null ? DataIndex[graphic].Groups[group] ?? _empty : _empty;
            }

            return _empty;
        }

        public bool IsReplacedByAnimationSequence(ushort graphic, out byte type)
        {
            return _animationSequenceReplacing.TryGetValue(graphic, out type);
        }

        public override AnimationFrameTexture GetTexture(uint id)
        {
            ResourceDictionary.TryGetValue(id, out AnimationFrameTexture aft);

            return aft;
        }

        public override void CleanResources()
        {
        }

        public void UpdateAnimationTable(uint flags)
        {
            for (ushort i = 0; i < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; i++)
            {
                bool replace = DataIndex[i].FileIndex >= 3;

                if (DataIndex[i].FileIndex == 1)
                    replace = (World.ClientLockedFeatures.Flags & LockedFeatureFlags.LordBlackthornsRevenge) != 0;
                else if (DataIndex[i].FileIndex == 2)
                    replace = (World.ClientLockedFeatures.Flags & LockedFeatureFlags.AgeOfShadows) != 0;

                if (replace)
                {
                    if (!DataIndex[i].HasBodyConversion)
                        DataIndex[i].GraphicConversion = (ushort)(DataIndex[i].GraphicConversion & ~0x8000);
                }
            }
        }

        [MethodImpl(256)]
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

        [MethodImpl(256)]
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

        [MethodImpl(256)]
        public void FixSittingDirection(ref byte layerDirection, ref bool mirror, ref int x, ref int y)
        {
            ref readonly var data = ref SittingInfos[SittingValue - 1];

            switch (Direction)
            {
                case 7:
                case 0:
                    {
                        if (data.Direction1 == -1)
                        {
                            if (Direction == 7)
                                Direction = (byte)data.Direction4;
                            else
                                Direction = (byte)data.Direction2;
                        }
                        else
                            Direction = (byte)data.Direction1;

                        break;
                    }

                case 1:
                case 2:
                    {
                        if (data.Direction2 == -1)
                        {
                            if (Direction == 1)
                                Direction = (byte)data.Direction1;
                            else
                                Direction = (byte)data.Direction3;
                        }
                        else
                            Direction = (byte)data.Direction2;

                        break;
                    }

                case 3:
                case 4:
                    {
                        if (data.Direction3 == -1)
                        {
                            if (Direction == 3)
                                Direction = (byte)data.Direction2;
                            else
                                Direction = (byte)data.Direction4;
                        }
                        else
                            Direction = (byte)data.Direction3;

                        break;
                    }

                case 5:
                case 6:
                    {
                        if (data.Direction4 == -1)
                        {
                            if (Direction == 5)
                                Direction = (byte)data.Direction3;
                            else
                                Direction = (byte)data.Direction1;
                        }
                        else
                            Direction = (byte)data.Direction4;

                        break;
                    }
            }

            layerDirection = Direction;
            byte dir = Direction;
            GetSittingAnimDirection(ref dir, ref mirror, ref x, ref y);
            Direction = dir;

            const int SITTING_OFFSET_X = 8;

            int offsX = SITTING_OFFSET_X;

            if (mirror)
            {
                if (Direction == 3)
                {
                    y += 25 + data.MirrorOffsetY;
                    x += offsX - 4;
                }
                else
                    y += data.OffsetY + 9;
            }
            else
            {
                if (Direction == 3)
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


        [MethodImpl(256)]
        public ANIMATION_GROUPS GetGroupIndex(ushort graphic, bool isequip = false)
        {
            if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                return ANIMATION_GROUPS.AG_HIGHT;

            switch (DataIndex[graphic].Type)
            {
                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    return ANIMATION_GROUPS.AG_LOW;

                case ANIMATION_GROUPS_TYPE.MONSTER:
                case ANIMATION_GROUPS_TYPE.SEA_MONSTER:

                    return ANIMATION_GROUPS.AG_HIGHT;

                case ANIMATION_GROUPS_TYPE.HUMAN:
                case ANIMATION_GROUPS_TYPE.EQUIPMENT:

                    return ANIMATION_GROUPS.AG_PEOPLE;
            }

            return ANIMATION_GROUPS.AG_HIGHT;
        }

        [MethodImpl(256)]
        public byte GetDieGroupIndex(ushort id, bool second, bool isRunning = false)
        {
            ANIMATION_FLAGS flags = (ANIMATION_FLAGS)DataIndex[id].Flags;

            switch (DataIndex[id].Type)
            {
                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    if ((flags & ANIMATION_FLAGS.AF_USE_2_IF_HITTED_WHILE_RUNNING) != 0 || (flags & ANIMATION_FLAGS.AF_CAN_FLYING) != 0)
                        return 2;

                    if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                        return (byte)(second ? 3 : 2);

                    return (byte)(second ? LOW_ANIMATION_GROUP.LAG_DIE_2 : LOW_ANIMATION_GROUP.LAG_DIE_1);

                case ANIMATION_GROUPS_TYPE.SEA_MONSTER:

                    {
                        if (!isRunning)
                            return 8;

                        goto case ANIMATION_GROUPS_TYPE.MONSTER;
                    }

                case ANIMATION_GROUPS_TYPE.MONSTER:

                    if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                        return (byte) (second ? 3 : 2);

                    return (byte)(second ? HIGHT_ANIMATION_GROUP.HAG_DIE_2 : HIGHT_ANIMATION_GROUP.HAG_DIE_1);

                case ANIMATION_GROUPS_TYPE.HUMAN:
                case ANIMATION_GROUPS_TYPE.EQUIPMENT:

                    return (byte)(second ? PEOPLE_ANIMATION_GROUP.PAG_DIE_2 : PEOPLE_ANIMATION_GROUP.PAG_DIE_1);
            }

            return 0;
        }

        [MethodImpl(256)]
        public bool AnimationExists(ushort graphic, byte group, bool isCorpse = false)
        {
            if (graphic < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && group < 100)
            {
                ushort hue = 0;
                AnimationDirection direction = isCorpse ? FileManager.Animations.GetCorpseAnimationGroup(ref graphic, ref group, ref hue)?.Direction[0] : FileManager.Animations.GetBodyAnimationGroup(ref graphic, ref group, ref hue, true)?.Direction[0];


                return direction != null && (direction.Address != 0 && direction.Size != 0 ||
                                             direction.IsUOP);
            }

            return false;
        }


        public bool LoadDirectionGroup(ref AnimationDirection animDir)
        {
            if (animDir.FileIndex == -1 && animDir.Address == -1)
                return false;

            if (animDir.IsUOP || animDir.Address == 0 && animDir.Size == 0)
            {
                var animData = DataIndex[AnimID].GetUopGroup(AnimGroup);

                if (animData == null || animData.Offset == 0)
                    return false;

                return TryReadUOPAnimDimension(ref animDir);
            }

            if (animDir.Address == 0 && animDir.Size == 0)
                return false;

            UOFileMul file = _files[animDir.FileIndex];
            file.Seek(animDir.Address);
            ReadFramesPixelData(ref animDir, file);

            return true;
        }

        private unsafe bool TryReadUOPAnimDimension(ref AnimationDirection animDirection)
        {
            var animData = DataIndex[AnimID].GetUopGroup(AnimGroup); //ref DataIndex[AnimID].Groups[AnimGroup];

            if (animData.FileIndex == 0 && animData.CompressedLength == 0 && animData.DecompressedLength == 0 && animData.Offset == 0)
            {
                Log.Message(LogTypes.Warning, "uop animData is null");

                return false;
            }

            animDirection.LastAccessTime = Engine.Ticks;
            int decLen = (int)animData.DecompressedLength;
            var file = _filesUop[animData.FileIndex];
            file.Seek(animData.Offset);

            byte[] decbuffer = file.GetData((int) animData.CompressedLength, decLen);


            fixed (byte* ptr = decbuffer)
            {
                DataReader reader = new DataReader();

                reader.SetData(ptr, decLen);
                reader.Skip(32);

                int frameCount = reader.ReadInt();
                int dataStart = reader.ReadInt();
                reader.Seek(dataStart);

                //List<UOPFrameData> pixelDataOffsets = new List<UOPFrameData>();

                UOPFrameData[] pixelDataOffsets = new UOPFrameData[frameCount];

                for (int i = 0; i < frameCount; i++)
                {
                    uint start = (uint) reader.Position;
                    ushort group = reader.ReadUShort();
                    short frameID = reader.ReadShort();
                    reader.Skip(8);
                    uint pixelOffset = reader.ReadUInt();
                    //int vsize = pixelDataOffsets.Count;

                    ref UOPFrameData data = ref pixelDataOffsets[i];
                    data.DataStart = start;
                    data.PixelDataOffset = pixelOffset;

                    //if (vsize + 1 < data.FrameID)
                    //{
                    //    while (vsize + 1 != data.FrameID)
                    //    {
                    //        pixelDataOffsets.Add(new UOPFrameData());
                    //        vsize++;
                    //    }
                    //}

                    //pixelDataOffsets.Add(data);
                }

                //int vectorSize = pixelDataOffsets.Count;
                //if (vectorSize < 50)
                //{
                //    while (vectorSize != 50)
                //    {
                //        pixelDataOffsets.Add(new UOPFrameData());
                //        vectorSize++;
                //    }
                //}

                animDirection.FrameCount = (byte)(pixelDataOffsets.Length / 5);
                int dirFrameStartIdx = animDirection.FrameCount * Direction;


                if (animDirection.Frames != null && animDirection.Frames.Length != 0)
                    Log.Message(LogTypes.Panic, "MEMORY LEAK UOP ANIM");

                animDirection.Frames = new AnimationFrameTexture[animDirection.FrameCount];

                for (int i = 0; i < animDirection.FrameCount; i++)
                {
                    if (animDirection.Frames[i] != null)
                        continue;

                    ref readonly UOPFrameData frameData = ref pixelDataOffsets[i + dirFrameStartIdx];

                    if (frameData.DataStart == 0)
                        continue;

                    reader.Seek((int)(frameData.DataStart + frameData.PixelDataOffset));
                    ushort* palette = (ushort*)reader.PositionAddress;
                    reader.Skip(512);
                    short imageCenterX = reader.ReadShort();
                    short imageCenterY = reader.ReadShort();
                    short imageWidth = reader.ReadShort();
                    short imageHeight = reader.ReadShort();

                    if (imageWidth == 0 || imageHeight == 0)
                    {
                        Log.Message(LogTypes.Warning, "frame size is null");

                        continue;
                    }

                    ushort[] data = new ushort[imageWidth * imageHeight];

                    fixed (ushort* ptrData = data)
                    {
                        int header;

                        const int DOUBLE_XOR = (0x200 << 22) | (0x200 << 12);

                        while ((header = reader.ReadInt()) != 0x7FFF7FFF)
                        {
                            header ^= DOUBLE_XOR;
                            int x = ((header >> 22) & 0x3FF) + imageCenterX - 0x200;
                            int y = ((header >> 12) & 0x3FF) + imageCenterY + imageHeight - 0x200;

                            ushort* cur = ptrData + y * imageWidth + x;
                            ushort* end = cur + (header & 0xFFF);
                            int filecounter = 0;

                            byte* filedata = (byte*)reader.PositionAddress;
                            reader.Skip(header & 0xFFF);
                            while (cur < end)
                            {
                                *cur++ = (ushort)(0x8000 | palette[filedata[filecounter++]]);
                            }
                        }
                    }

                    AnimationFrameTexture f = new AnimationFrameTexture(imageWidth, imageHeight)
                    {
                        CenterX = imageCenterX,
                        CenterY = imageCenterY
                    };

                    f.PushData(data);
                    animDirection.Frames[i] = f;
                }

                _usedTextures.Add(animDirection);

                reader.ReleaseData();
            }


            return true;
        }

        private unsafe void ReadFramesPixelData(ref AnimationDirection animDir, UOFile reader)
        {
            animDir.LastAccessTime = Engine.Ticks;

            ushort* palette = (ushort*)reader.PositionAddress;
            reader.Skip(512);

            long dataStart = reader.Position;
            uint frameCount = reader.ReadUInt();
            animDir.FrameCount = (byte)frameCount;
            uint* frameOffset = (uint*)reader.PositionAddress;


            if (animDir.Frames != null && animDir.Frames.Length != 0)
                Log.Message(LogTypes.Panic, "MEMORY LEAK MUL ANIM");


            animDir.Frames = new AnimationFrameTexture[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                if (animDir.Frames[i] != null)
                    continue;

                reader.Seek(dataStart + frameOffset[i]);

                short imageCenterX = reader.ReadShort();
                short imageCenterY = reader.ReadShort();
                short imageWidth = reader.ReadShort();
                short imageHeight = reader.ReadShort();

                if (imageWidth == 0 || imageHeight == 0)
                    continue;

                ushort[] data = new ushort[imageWidth * imageHeight];

                fixed (ushort* ptrData = data)
                {
                    ushort* dataRef = ptrData;

                    int header;

                    const int DOUBLE_XOR = (0x200 << 22) | (0x200 << 12);

                    while ((header = reader.ReadInt()) != 0x7FFF7FFF)
                    {
                        header ^= DOUBLE_XOR;
                        int x = ((header >> 22) & 0x3FF) + imageCenterX - 0x200;
                        int y = ((header >> 12) & 0x3FF) + imageCenterY + imageHeight - 0x200;

                        ushort* cur = ptrData + y * imageWidth + x;
                        ushort* end = cur + (header & 0xFFF);
                        int filecounter = 0;

                        byte* filedata = (byte*)reader.PositionAddress;
                        reader.Skip(header & 0xFFF);
                        while (cur < end)
                        {
                            *cur++ = (ushort)(0x8000 | palette[filedata[filecounter++]]);
                        }
                    }
                }

                AnimationFrameTexture f = new AnimationFrameTexture(imageWidth, imageHeight)
                {
                    CenterX = imageCenterX,
                    CenterY = imageCenterY
                };

                f.PushData(data);

                animDir.Frames[i] = f;
            }

            _usedTextures.Add(animDir);
        }

        public void GetAnimationDimensions(sbyte animIndex, ushort graphic, byte dir, byte animGroup, bool ismounted, byte frameIndex, out int centerX, out int centerY, out int width, out int height)
        {
            dir &= 0x7F;
            bool mirror = false;
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
                frameIndex = (byte)animIndex;
            FileManager.Animations.GetAnimationDimensions(frameIndex, graphic, dir, animGroup, out centerX, out centerY, out width, out height);
            if (centerX == 0 && centerY == 0 && width == 0 && height == 0) height = ismounted ? 100 : 60;
        }

        public unsafe void GetAnimationDimensions(byte frameIndex, ushort id, byte dir, byte animGroup, out int x, out int y, out int w, out int h)
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
                    AnimationDirection direction = FileManager.Animations.GetBodyAnimationGroup(ref id, ref animGroup, ref hue, true).Direction[dir];
                    int fc = direction.FrameCount;

                    if (fc > 0)
                    {
                        if (frameIndex >= fc) frameIndex = 0;

                        if (direction.Frames != null)
                        {
                            AnimationFrameTexture animationFrameTexture = direction.Frames[frameIndex]; // GetTexture(direction.FramesHashes[frameIndex]);
                            x = animationFrameTexture.CenterX;
                            y = animationFrameTexture.CenterY;
                            w = animationFrameTexture.Width;
                            h = animationFrameTexture.Height;
                            _animDimensionCache[id] = new Rectangle(x, y, w, h);

                            return;
                        }
                    }
                }

                AnimationDirection direction1 = FileManager.Animations.GetBodyAnimationGroup(ref id, ref animGroup, ref hue, true).Direction[0];

                if (direction1.Address != 0 && direction1.Size != 0)
                {
                    if (!direction1.IsVerdata)
                    {
                        UOFileMul file = _files[direction1.FileIndex];
                        file.Seek(direction1.Address);
                        ReadFrameDimensionData(frameIndex, out x, out y, out w, out h, file);
                        _animDimensionCache[id] = new Rectangle(x, y, w, h);

                        return;
                    }
                }
                else if (direction1.IsUOP && FileManager.Animations.GetBodyAnimationGroup(ref id, ref animGroup, ref hue, true) is AnimationGroupUop animDataStruct)
                {
                    if (!(animDataStruct.FileIndex == 0 && animDataStruct.CompressedLength == 0 && animDataStruct.DecompressedLength == 0 && animDataStruct.Offset == 0))
                    {
                        int decLen = (int)animDataStruct.DecompressedLength;
                        var file = _filesUop[animDataStruct.FileIndex];
                        file.Seek(animDataStruct.Offset);
                        byte[] decbuffer = file.GetData((int) animDataStruct.CompressedLength, decLen);

                        fixed (byte* ptr = decbuffer)
                        {
                            DataReader reader = new DataReader();
                            reader.SetData(ptr, decLen);
                            reader.Skip(32);

                            int frameCount = reader.ReadInt();
                            int dataStart = reader.ReadInt();
                            reader.Seek(dataStart);

                            reader.Skip(2);
                            short frameID = reader.ReadShort();
                            reader.Skip(8);
                            uint pixelOffset = reader.ReadUInt();

                            reader.Seek((int)(dataStart + pixelOffset));
                            reader.Skip(512);
                            x = reader.ReadShort();
                            y = reader.ReadShort();
                            w = reader.ReadShort();
                            h = reader.ReadShort();
                            _animDimensionCache[id] = new Rectangle(x, y, w, h);
                            reader.ReleaseData();

                            return;
                        }
                    }
                }
            }

            x = 0;
            y = 0;
            w = 0;
            h = 0;
        }

        private unsafe void ReadFrameDimensionData(byte frameIndex, out int x, out int y, out int w, out int h, UOFile reader)
        {
            reader.Skip(512);
            long dataStart = reader.Position;
            uint frameCount = reader.ReadUInt();
            if (frameCount > 0 && frameIndex >= frameCount) frameIndex = 0;

            if (frameIndex < frameCount)
            {
                uint* frameOffset = (uint*)reader.PositionAddress;
                reader.Seek(dataStart + frameOffset[frameIndex]);
                x = reader.ReadShort();
                y = reader.ReadShort();
                w = reader.ReadShort();
                h = reader.ReadShort();
            }
            else
                x = y = w = h = 0;
        }

        public override void CleaUnusedResources()
        {
            int count = 0;
            long ticks = Engine.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            for (int i = 0; i < _usedTextures.Count; i++)
            {
                var t = _usedTextures[i];

                if (t == null)
                    _usedTextures.RemoveAt(i--);
                else if (t.LastAccessTime != 0 && t.LastAccessTime < ticks)
                {
                    for (int j = 0; j < t.FrameCount; j++)
                    {
                        ref var texture = ref t.Frames[j];

                        if (texture != null)
                        {
                            texture.Dispose();
                            texture = null;
                        }
                    }

                    t.FrameCount = 0;
                    t.Frames = null;
                    t.LastAccessTime = 0;

                    _usedTextures.RemoveAt(i--);

                    if (++count >= Constants.MAX_ANIMATIONS_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
                        break;
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _usedTextures.Count; i++)
            {
                var t = _usedTextures[i];

                if (t == null)
                {
                }
                else if (t.LastAccessTime != 0)
                {
                    for (int j = 0; j < t.FrameCount; j++)
                    {
                        ref var texture = ref t.Frames[j];

                        if (texture != null)
                        {
                            texture.Dispose();
                            texture = null;
                        }
                    }

                    t.FrameCount = 0;
                    t.Frames = null;
                    t.LastAccessTime = 0;
                }
            }

            if (_usedTextures.Count != 0)
                _usedTextures.Clear();
        }

        public readonly struct SittingInfoData
        {
            public SittingInfoData(ushort graphic, sbyte d1,
                                   sbyte d2, sbyte d3, sbyte d4,
                                   sbyte offsetY,
                                   sbyte mirrorOffsetY,
                                   bool drawback)
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
        }

        private struct UOPFrameData
        {
            public uint DataStart;
            public uint PixelDataOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly struct AnimIdxBlock
        {
            public readonly uint Position;
            public readonly uint Size;
            public readonly uint Unknown;
        }
    }

    public enum ANIMATION_GROUPS
    {
        AG_NONE = 0,
        AG_LOW,
        AG_HIGHT,
        AG_PEOPLE
    }

    public enum ANIMATION_GROUPS_TYPE
    {
        MONSTER = 0,
        SEA_MONSTER,
        ANIMAL,
        HUMAN,
        EQUIPMENT,
        UNKNOWN
    }

    public enum HIGHT_ANIMATION_GROUP
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

    public enum PEOPLE_ANIMATION_GROUP
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

    public enum LOW_ANIMATION_GROUP
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
        public AnimationGroup[] BodyConvGroups;
        public ushort Color;
        public ushort CorpseColor;

        public ushort CorpseGraphic;

        public byte FileIndex;
        public uint Flags;

        public ushort Graphic;

        public ushort GraphicConversion = 0x8000;

        // 100
        public AnimationGroup[] Groups;

        public bool IsValidMUL;
        public sbyte MountedHeightOffset;

        public ANIMATION_GROUPS_TYPE Type = ANIMATION_GROUPS_TYPE.UNKNOWN;
        public AnimationGroupUop[] UopGroups;


        public bool IsUOP => (Flags & (uint)ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0;

        public bool HasBodyConversion => (GraphicConversion & 0x8000) == 0 && BodyConvGroups != null;


        public AnimationGroupUop GetUopGroup(byte group)
        {
            return group < 100 && UopGroups != null ? UopGroups[_uopReplaceGroupIndex[group]] : null;
        }

        public void InitializeUOP()
        {
            if (_uopReplaceGroupIndex == null)
            {
                _uopReplaceGroupIndex = new byte[100];

                for (byte i = 0; i < 100; i++)
                    _uopReplaceGroupIndex[i] = i;
            }
        }

        public void ReplaceUopGroup(byte old, byte newG)
        {
            _uopReplaceGroupIndex[old] = newG;
        }

        public long CalculateOffset(ushort graphic, out int groupCount)
        {
            long result = 0;
            groupCount = 0;

            ANIMATION_GROUPS group = ANIMATION_GROUPS.AG_NONE;

            switch (Type)
            {
                case ANIMATION_GROUPS_TYPE.MONSTER:

                    if ((Flags & (uint)ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP) != 0)
                        group = ANIMATION_GROUPS.AG_PEOPLE;
                    else if ((Flags & (uint)ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
                        group = ANIMATION_GROUPS.AG_LOW;
                    else
                        group = ANIMATION_GROUPS.AG_HIGHT;

                    break;

                case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                    result = AnimationsLoader.CalculateHighGroupOffset(graphic);
                    groupCount = (int)LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;

                    break;

                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    if ((Flags & (uint)ANIMATION_FLAGS.AF_CALCULATE_OFFSET_LOW_GROUP_EXTENDED) != 0)
                    {
                        if ((Flags & (uint)ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP) != 0)
                            group = ANIMATION_GROUPS.AG_PEOPLE;
                        else if ((Flags & (uint)ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
                            group = ANIMATION_GROUPS.AG_LOW;
                        else
                            group = ANIMATION_GROUPS.AG_HIGHT;
                    }
                    else
                        group = ANIMATION_GROUPS.AG_LOW;

                    break;

                default:
                    group = ANIMATION_GROUPS.AG_PEOPLE;

                    break;
            }

            switch (group)
            {
                case ANIMATION_GROUPS.AG_LOW:
                    result = AnimationsLoader.CalculateLowGroupOffset(graphic);
                    groupCount = (int)LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;

                    break;

                case ANIMATION_GROUPS.AG_HIGHT:
                    result = AnimationsLoader.CalculateHighGroupOffset(graphic);
                    groupCount = (int)HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT;

                    break;

                case ANIMATION_GROUPS.AG_PEOPLE:
                    result = AnimationsLoader.CalculatePeopleGroupOffset(graphic);
                    groupCount = (int)PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;

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
        public AnimationFrameTexture[] Frames;
        public bool IsUOP;
        public bool IsVerdata;
        public long LastAccessTime;
        public uint Size;
    }

    internal readonly struct EquipConvData
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
    }
}