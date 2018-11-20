#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.Runtime.InteropServices;

using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.IO.Resources
{
    public static class Animations
    {
        public const int MAX_ANIMATIONS_DATA_INDEX_COUNT = 2048;
        private static readonly UOFileMul[] _files = new UOFileMul[5];
        private static readonly UOFileUopAnimation[] _filesUop = new UOFileUopAnimation[4];
        private static readonly List<Tuple<ushort, byte>>[] _groupReplaces = new List<Tuple<ushort, byte>>[2]
        {
            new List<Tuple<ushort, byte>>(), new List<Tuple<ushort, byte>>()
        };
        private static readonly Dictionary<ushort, Dictionary<ushort, EquipConvData>> _equipConv = new Dictionary<ushort, Dictionary<ushort, EquipConvData>>();
        private static byte _animGroupCount = (int) PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;
        private static readonly DataReader _reader = new DataReader();
        private static readonly PixelPicking _picker = new PixelPicking();
        private static readonly List<ToRemoveInfo> _usedTextures = new List<ToRemoveInfo>();
        private static readonly Dictionary<Graphic, Rectangle> _animDimensionCache = new Dictionary<Graphic, Rectangle>();

        public static ushort Color { get; set; }

        public static byte AnimGroup { get; set; }

        public static byte Direction { get; set; }

        public static ushort AnimID { get; set; }

        public static IndexAnimation[] DataIndex { get; } = new IndexAnimation[MAX_ANIMATIONS_DATA_INDEX_COUNT];

        public static IReadOnlyDictionary<ushort, Dictionary<ushort, EquipConvData>> EquipConversions => _equipConv;

        public static IReadOnlyList<Tuple<ushort, byte>>[] GroupReplaces => _groupReplaces;

        public static void Load()
        {
            Dictionary<ulong, UOPAnimationData> hashes = new Dictionary<ulong, UOPAnimationData>();

            for (int i = 0; i < 5; i++)
            {
                string pathmul = Path.Combine(FileManager.UoFolderPath, "anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".mul");
                string pathidx = Path.Combine(FileManager.UoFolderPath, "anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".idx");
                if (File.Exists(pathmul) && File.Exists(pathidx)) _files[i] = new UOFileMul(pathmul, pathidx, 0, i == 0 ? 6 : 0, false);

                if (i > 0 && FileManager.ClientVersion >= ClientVersions.CV_7000)
                {
                    string pathuop = Path.Combine(FileManager.UoFolderPath, $"AnimationFrame{i}.uop");

                    if (File.Exists(pathuop))
                    {
                        _filesUop[i - 1] = new UOFileUopAnimation(pathuop, i - 1);
                        _filesUop[i - 1].LoadEx(ref hashes);
                    }
                }
            }

            if (FileManager.ClientVersion >= ClientVersions.CV_500A)
            {
                string[] typeNames = new string[5]
                {
                    "monster", "sea_monster", "animal", "human", "equipment"
                };

                using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "mobtypes.txt"))))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line.Length == 0 || line.Length < 3 || line[0] == '#')
                            continue;

                        string[] parts = line.Split(new[]
                        {
                            '\t', ' '
                        }, StringSplitOptions.RemoveEmptyEntries);
                        int id = int.Parse(parts[0]);

                        if (id >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
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
                                DataIndex[id].Type = (ANIMATION_GROUPS_TYPE) i;
                                DataIndex[id].Flags = 0x80000000 | number;

                                break;
                            }
                        }
                    }
                }
            }

            int animIdxBlockSize = Marshal.SizeOf<AnimIdxBlock>();
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

            for (int i = 0; i < MAX_ANIMATIONS_DATA_INDEX_COUNT; i++)
            {
                ANIMATION_GROUPS_TYPE groupTye = ANIMATION_GROUPS_TYPE.UNKNOWN;
                int findID = 0;

                if (i >= 200)
                {
                    if (i >= 400)
                    {
                        groupTye = ANIMATION_GROUPS_TYPE.HUMAN;
                        findID = ((i - 400) * 175 + 35000) * animIdxBlockSize;
                    }
                    else
                    {
                        groupTye = ANIMATION_GROUPS_TYPE.ANIMAL;
                        findID = ((i - 200) * 65 + 22000) * animIdxBlockSize;
                    }
                }
                else
                {
                    groupTye = ANIMATION_GROUPS_TYPE.MONSTER;
                    findID = i * 110 * animIdxBlockSize;
                }

                DataIndex[i].Graphic = (ushort) i;
                int count = 0;

                switch (groupTye)
                {
                    case ANIMATION_GROUPS_TYPE.MONSTER:
                    case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                        count = (int) HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT;

                        break;
                    case ANIMATION_GROUPS_TYPE.HUMAN:
                    case ANIMATION_GROUPS_TYPE.EQUIPMENT:
                        count = (int) PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;

                        break;
                    case ANIMATION_GROUPS_TYPE.ANIMAL:
                    default:
                        count = (int) LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;

                        break;
                }

                DataIndex[i].Type = groupTye;
                IntPtr address = _files[0].IdxFile.StartAddress + findID;
                DataIndex[i].Groups = new AnimationGroup[100];

                for (byte j = 0; j < 100; j++)
                {
                    DataIndex[i].Groups[j].Direction = new AnimationDirection[5];

                    if (j >= count)
                        continue;
                    int offset = j * 5;

                    for (byte d = 0; d < 5; d++)
                    {
                        unsafe
                        {
                            AnimIdxBlock* aidx = (AnimIdxBlock*) (address + (offset + d) * animIdxBlockSize);

                            if ((long) aidx >= maxAddress0)
                                break;

                            if (aidx->Size > 0 && aidx->Position != 0xFFFFFFFF && aidx->Size != 0xFFFFFFFF)
                            {
                                DataIndex[i].Groups[j].Direction[d].BaseAddress = aidx->Position;
                                DataIndex[i].Groups[j].Direction[d].BaseSize = aidx->Size;
                                DataIndex[i].Groups[j].Direction[d].Address = DataIndex[i].Groups[j].Direction[d].BaseAddress;
                                DataIndex[i].Groups[j].Direction[d].Size = DataIndex[i].Groups[j].Direction[d].BaseSize;
                            }
                        }
                    }
                }
            }

            void readAnimDef(TextReader reader, int idx)
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length <= 0 || line[0] == '#' || !char.IsNumber(line[0]))
                        continue;

                    string[] parts = line.Split(new[]
                    {
                        '\t', ' ', '#'
                    }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 2)
                        continue;
                    ushort group = ushort.Parse(parts[0]);
                    int first = parts[1].IndexOf("{", StringComparison.Ordinal);
                    int last = parts[1].IndexOf("}", StringComparison.Ordinal);
                    int replaceGroup = int.Parse(parts[1].Substring(first + 1, last - 1));
                    _groupReplaces[idx].Add(new Tuple<ushort, byte>(group, (byte) replaceGroup));
                }
            }

            using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "Anim1.def")))) readAnimDef(reader, 0);
            using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "Anim2.def")))) readAnimDef(reader, 1);

            if (FileManager.ClientVersion < ClientVersions.CV_305D)
                return;

            using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "Equipconv.def"))))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length <= 0 || line[0] == '#' || !char.IsNumber(line[0]))
                        continue;

                    string[] parts = line.Split(new[]
                    {
                        '\t', ' '
                    }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 5)
                    {
                        ushort body = (ushort) int.Parse(parts[0]);

                        if (body >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            continue;
                        ushort graphic = (ushort) int.Parse(parts[1]);

                        if (graphic >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            continue;
                        ushort newgraphic = (ushort) int.Parse(parts[2]);

                        if (newgraphic >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            newgraphic = graphic;
                        int gump = int.Parse(parts[3]);

                        if (gump > ushort.MaxValue)
                            continue;

                        if (gump == 0)
                            gump = graphic;
                        else if (gump == 0xFFFF)
                            gump = newgraphic;
                        ushort color = (ushort) int.Parse(parts[4]);

                        if (!_equipConv.TryGetValue(body, out Dictionary<ushort, EquipConvData> dict))
                        {
                            _equipConv.Add(body, new Dictionary<ushort, EquipConvData>());

                            if (!_equipConv.TryGetValue(body, out dict))
                                continue;
                        }

                        dict.Add(graphic, new EquipConvData(newgraphic, (ushort) gump, color));
                    }
                }
            }

            using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "Bodyconv.def"))))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length <= 0 || line[0] == '#' || !char.IsNumber(line[0]))
                        continue;

                    string[] parts = line.Split(new[]
                    {
                        '\t', ' ', '#'
                    }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 2)
                    {
                        ushort index = ushort.Parse(parts[0]);

                        if (index >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            continue;

                        int[] anim =
                        {
                            int.Parse(parts[1]), -1, -1, -1
                        };

                        if (parts.Length >= 3)
                        {
                            anim[1] = int.Parse(parts[2]);

                            if (parts.Length >= 4)
                            {
                                anim[2] = int.Parse(parts[3]);

                                if (parts.Length >= 5)
                                    anim[3] = int.Parse(parts[4]);
                            }
                        }

                        int startAnimID = -1;
                        int animFile = 0;
                        ushort realAnimID = 0;
                        sbyte mountedHeightOffset = 0;
                        ANIMATION_GROUPS_TYPE groupType = ANIMATION_GROUPS_TYPE.UNKNOWN;

                        if (anim[0] != -1 && maxAddress2.HasValue && maxAddress2 != 0)
                        {
                            animFile = 1;
                            realAnimID = (ushort) anim[0];

                            if (realAnimID == 68)
                                realAnimID = 122;

                            if (realAnimID >= 200)
                            {
                                startAnimID = (realAnimID - 200) * 65 + 22000;
                                groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                            }
                            else
                            {
                                startAnimID = realAnimID * 110;
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                        }
                        else if (anim[1] != -1 && maxAddress3.HasValue && maxAddress3 != 0)
                        {
                            animFile = 2;
                            realAnimID = (ushort) anim[1];

                            if (realAnimID >= 200)
                            {
                                if (realAnimID >= 400)
                                {
                                    startAnimID = (realAnimID - 400) * 175 + 35000;
                                    groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                                }
                                else
                                {
                                    startAnimID = (realAnimID - 200) * 110 + 22000;
                                    groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                                }
                            }
                            else
                            {
                                startAnimID = realAnimID * 65 + 9000;
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                        }
                        else if (anim[2] != -1 && maxAddress4.HasValue && maxAddress4 != 0)
                        {
                            animFile = 3;
                            realAnimID = (ushort) anim[2];

                            if (realAnimID >= 200)
                            {
                                if (realAnimID >= 400)
                                {
                                    startAnimID = (realAnimID - 400) * 175 + 35000;
                                    groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                                }
                                else
                                {
                                    startAnimID = (realAnimID - 200) * 65 + 22000;
                                    groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                                }
                            }
                            else
                            {
                                startAnimID = realAnimID * 110;
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                        }
                        else if (anim[3] != -1 && maxAddress5.HasValue && maxAddress5 != 0)
                        {
                            animFile = 4;
                            realAnimID = (ushort) anim[3];
                            mountedHeightOffset = -9;

                            if (realAnimID == 34)
                                startAnimID = (realAnimID - 200) * 65 + 22000;
                            else if (realAnimID >= 200)
                            {
                                if (realAnimID >= 400)
                                {
                                    startAnimID = (realAnimID - 400) * 175 + 35000;
                                    groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                                }
                                else
                                {
                                    startAnimID = (realAnimID - 200) * 65 + 22000;
                                    groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                                }
                            }
                            else
                            {
                                startAnimID = realAnimID * 110;
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                        }

                        if (startAnimID != -1)
                        {
                            startAnimID = startAnimID * animIdxBlockSize;
                            UOFile currentIdxFile = _files[animFile].IdxFile;

                            if ((uint) startAnimID < currentIdxFile.Length)
                            {
                                DataIndex[index].MountedHeightOffset = mountedHeightOffset;

                                if (FileManager.ClientVersion < ClientVersions.CV_500A || groupType == ANIMATION_GROUPS_TYPE.UNKNOWN)
                                {
                                    if (realAnimID >= 200)
                                    {
                                        if (realAnimID >= 400)
                                            DataIndex[index].Type = ANIMATION_GROUPS_TYPE.HUMAN;
                                        else
                                            DataIndex[index].Type = ANIMATION_GROUPS_TYPE.ANIMAL;
                                    }
                                    else
                                        DataIndex[index].Type = ANIMATION_GROUPS_TYPE.MONSTER;
                                }
                                else if (groupType != ANIMATION_GROUPS_TYPE.UNKNOWN) DataIndex[index].Type = groupType;

                                int count = 0;

                                switch (DataIndex[index].Type)
                                {
                                    case ANIMATION_GROUPS_TYPE.MONSTER:
                                    case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                                        count = (int) HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT;

                                        break;
                                    case ANIMATION_GROUPS_TYPE.HUMAN:
                                    case ANIMATION_GROUPS_TYPE.EQUIPMENT:
                                        count = (int) PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;

                                        break;
                                    case ANIMATION_GROUPS_TYPE.ANIMAL:
                                    default:
                                        count = (int) LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;

                                        break;
                                }

                                IntPtr address = currentIdxFile.StartAddress + startAnimID;
                                IntPtr maxaddress = currentIdxFile.StartAddress + (int) currentIdxFile.Length;

                                for (int j = 0; j < count; j++)
                                {
                                    int offset = j * 5;

                                    for (byte d = 0; d < 5; d++)
                                    {
                                        unsafe
                                        {
                                            AnimIdxBlock* aidx = (AnimIdxBlock*) (address + (offset + d) * animIdxBlockSize);

                                            if ((long) aidx >= (long) maxaddress)
                                                break;

                                            if (aidx->Size > 0 && aidx->Position != 0xFFFFFFFF && aidx->Size != 0xFFFFFFFF)
                                            {
                                                DataIndex[index].Groups[j].Direction[d].PatchedAddress = aidx->Position;
                                                DataIndex[index].Groups[j].Direction[d].PatchedSize = aidx->Size;
                                                DataIndex[index].Groups[j].Direction[d].FileIndex = animFile;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "Body.def"))))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length <= 0 || line[0] == '#' || !char.IsNumber(line[0]))
                        continue;
                    int first = line.IndexOf("{");
                    int last = line.IndexOf("}");
                    string part0 = line.Substring(0, first);
                    string part1 = line.Substring(first + 1, last - first - 1);
                    int canc = line.IndexOf('#');
                    string part2 = line.Substring(last + 1, canc >= 0 ? canc - last - 1 : line.Length - last - 1);
                    int comma = part1.IndexOf(',');

                    if (comma > -1)
                        part1 = part1.Substring(0, comma).Trim();
                    ushort index = ushort.Parse(part0);

                    if (index >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        continue;
                    ushort checkIndex = ushort.Parse(part1);

                    if (checkIndex >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        continue;
                    int count = 0;

                    int[] ignoreGroups =
                    {
                        -1, -1
                    };

                    switch (DataIndex[checkIndex].Type)
                    {
                        case ANIMATION_GROUPS_TYPE.MONSTER:
                        case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                            count = (int) HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT;
                            ignoreGroups[0] = (int) HIGHT_ANIMATION_GROUP.HAG_DIE_1;
                            ignoreGroups[1] = (int) HIGHT_ANIMATION_GROUP.HAG_DIE_2;

                            break;
                        case ANIMATION_GROUPS_TYPE.HUMAN:
                        case ANIMATION_GROUPS_TYPE.EQUIPMENT:
                            count = (int) PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;
                            ignoreGroups[0] = (int) PEOPLE_ANIMATION_GROUP.PAG_DIE_1;
                            ignoreGroups[1] = (int) PEOPLE_ANIMATION_GROUP.PAG_DIE_2;

                            break;
                        case ANIMATION_GROUPS_TYPE.ANIMAL:
                            count = (int) LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;
                            ignoreGroups[0] = (int) LOW_ANIMATION_GROUP.LAG_DIE_1;
                            ignoreGroups[1] = (int) LOW_ANIMATION_GROUP.LAG_DIE_2;

                            break;
                    }

                    for (int j = 0; j < count; j++)
                    {
                        if (j == ignoreGroups[0] || j == ignoreGroups[1])
                            continue;

                        for (byte d = 0; d < 5; d++)
                        {
                            DataIndex[index].Groups[j].Direction[d].BaseAddress = DataIndex[checkIndex].Groups[j].Direction[d].BaseAddress;
                            DataIndex[index].Groups[j].Direction[d].BaseSize = DataIndex[checkIndex].Groups[j].Direction[d].BaseSize;
                            DataIndex[index].Groups[j].Direction[d].Address = DataIndex[index].Groups[j].Direction[d].BaseAddress;
                            DataIndex[index].Groups[j].Direction[d].Size = DataIndex[index].Groups[j].Direction[d].BaseSize;

                            if (DataIndex[index].Groups[j].Direction[d].PatchedAddress <= 0)
                            {
                                DataIndex[index].Groups[j].Direction[d].PatchedAddress = DataIndex[checkIndex].Groups[j].Direction[d].PatchedAddress;
                                DataIndex[index].Groups[j].Direction[d].PatchedSize = DataIndex[checkIndex].Groups[j].Direction[d].PatchedSize;
                                DataIndex[index].Groups[j].Direction[d].FileIndex = DataIndex[checkIndex].Groups[j].Direction[d].FileIndex;
                            }

                            if (DataIndex[index].Groups[j].Direction[d].BaseAddress <= 0)
                            {
                                DataIndex[index].Groups[j].Direction[d].BaseAddress = DataIndex[index].Groups[j].Direction[d].PatchedAddress;
                                DataIndex[index].Groups[j].Direction[d].BaseSize = DataIndex[index].Groups[j].Direction[d].PatchedSize;
                                DataIndex[index].Groups[j].Direction[d].Address = DataIndex[index].Groups[j].Direction[d].BaseAddress;
                                DataIndex[index].Groups[j].Direction[d].Size = DataIndex[index].Groups[j].Direction[d].BaseSize;
                            }
                        }
                    }

                    DataIndex[index].Type = DataIndex[checkIndex].Type;
                    DataIndex[index].Flags = DataIndex[checkIndex].Flags;
                    DataIndex[index].Graphic = checkIndex;
                    DataIndex[index].Color = ushort.Parse(part2);
                }
            }

            using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "Corpse.def"))))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length <= 0 || line[0] == '#' || !char.IsNumber(line[0]))
                        continue;

                    string[] parts = line.Split(new[]
                    {
                        '\t', ' '
                    }, StringSplitOptions.RemoveEmptyEntries);
                    int first = line.IndexOf("{");
                    int last = line.IndexOf("}");
                    string part0 = line.Substring(0, first);
                    string part1 = line.Substring(first + 1, last - first - 1);
                    string part2 = line.Substring(last + 1);
                    int comma = part1.IndexOf(',');

                    if (comma > -1)
                        part1 = part1.Substring(0, comma).Trim();
                    ushort index = ushort.Parse(part0);

                    if (index >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        continue;
                    ushort checkIndex = ushort.Parse(part1);

                    if (checkIndex >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        continue;

                    int[] ignoreGroups =
                    {
                        -1, -1
                    };

                    switch (DataIndex[checkIndex].Type)
                    {
                        case ANIMATION_GROUPS_TYPE.MONSTER:
                        case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                            ignoreGroups[0] = (int) HIGHT_ANIMATION_GROUP.HAG_DIE_1;
                            ignoreGroups[1] = (int) HIGHT_ANIMATION_GROUP.HAG_DIE_2;

                            break;
                        case ANIMATION_GROUPS_TYPE.HUMAN:
                        case ANIMATION_GROUPS_TYPE.EQUIPMENT:
                            ignoreGroups[0] = (int) PEOPLE_ANIMATION_GROUP.PAG_DIE_1;
                            ignoreGroups[1] = (int) PEOPLE_ANIMATION_GROUP.PAG_DIE_2;

                            break;
                        case ANIMATION_GROUPS_TYPE.ANIMAL:
                            ignoreGroups[0] = (int) LOW_ANIMATION_GROUP.LAG_DIE_1;
                            ignoreGroups[1] = (int) LOW_ANIMATION_GROUP.LAG_DIE_2;

                            break;
                    }

                    if (ignoreGroups[0] == -1)
                        continue;

                    for (byte j = 0; j < 2; j++)
                    {
                        for (byte d = 0; d < 5; d++)
                        {
                            DataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress = DataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].BaseAddress;
                            DataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseSize = DataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].BaseSize;
                            DataIndex[index].Groups[ignoreGroups[j]].Direction[d].Address = DataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress;
                            DataIndex[index].Groups[ignoreGroups[j]].Direction[d].Size = DataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseSize;

                            if (DataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedAddress <= 0)
                            {
                                DataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedAddress = DataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].PatchedAddress;
                                DataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedSize = DataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].PatchedSize;
                                DataIndex[index].Groups[ignoreGroups[j]].Direction[d].FileIndex = DataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].FileIndex;
                            }

                            if (DataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress <= 0)
                            {
                                DataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress = DataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedAddress;
                                DataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseSize = DataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedSize;
                                DataIndex[index].Groups[ignoreGroups[j]].Direction[d].Address = DataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress;
                                DataIndex[index].Groups[ignoreGroups[j]].Direction[d].Size = DataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseSize;
                            }
                        }
                    }

                    DataIndex[index].Type = DataIndex[checkIndex].Type;
                    DataIndex[index].Flags = DataIndex[checkIndex].Flags;
                    DataIndex[index].Graphic = checkIndex;
                    DataIndex[index].Color = ushort.Parse(part2);
                }
            }

            byte maxGroup = 0;

            for (int animID = 0; animID < MAX_ANIMATIONS_DATA_INDEX_COUNT; animID++)
            {
                for (byte grpID = 0; grpID < 100; grpID++)
                {
                    string hashstring = $"build/animationlegacyframe/{animID:D6}/{grpID:D2}.bin";
                    ulong hash = UOFileUop.CreateHash(hashstring);

                    if (hashes.TryGetValue(hash, out UOPAnimationData data))
                    {
                        if (grpID > maxGroup)
                            maxGroup = grpID;
                        DataIndex[animID].IsUOP = true;
                        DataIndex[animID].Groups[grpID].UOPAnimData = data;

                        for (byte dirID = 0; dirID < 5; dirID++)
                        {
                            DataIndex[animID].Groups[grpID].Direction[dirID].IsUOP = true;
                            DataIndex[animID].Groups[grpID].Direction[dirID].BaseAddress = 0;
                            DataIndex[animID].Groups[grpID].Direction[dirID].Address = 0;
                        }
                    }
                }
            }

            if (_animGroupCount < maxGroup)
                _animGroupCount = maxGroup;

            if (FileManager.ClientVersion > ClientVersions.CV_60144)
            {
                // AnimationSequence.uop
                // https://github.com/AimedNuu/OrionUO/blob/f27a29806aab9379fa004af953832f3e2ffe248d/OrionUO/Managers/FileManager.cpp#L738
                UOFileUop animSeq = new UOFileUop(Path.Combine(FileManager.UoFolderPath, "AnimationSequence.uop"), ".bin", MAX_ANIMATIONS_DATA_INDEX_COUNT);

                //LogFile file = new LogFile(Bootstrap.ExeDirectory, "file.txt");

                for (int i = 0; i < animSeq.Entries.Length; i++)
                {
                    UOFileIndex3D entry = animSeq.Entries[i];

                    if (entry.Length > 0 && entry.Offset > 0)
                    {
                        animSeq.Seek(entry.Offset);
                        byte[] buffer = animSeq.ReadArray<byte>(entry.Length);
                        int decLen = entry.DecompressedLength;
                        byte[] decbuffer = new byte[decLen];
                        ZLib.Decompress(buffer, 0, decbuffer, decbuffer.Length);

                        //ZLib.Unpack(decbuffer, ref decLen, buffer, entry.Length);
                        _reader.SetData(decbuffer, decbuffer.Length);
                        uint animID = _reader.ReadUInt();
                        ref IndexAnimation index = ref DataIndex[animID];

                        if (!index.IsUOP)
                            continue;
                        _reader.Skip(48);
                        int replaces = _reader.ReadInt();

                        if (replaces == 48 || replaces == 68)
                            continue;

                        //StringBuilder sb = new StringBuilder();
                        //sb.AppendLine($"- 0x{animID:X4},\ttype: {replaces}");

                        //switch (replaces)
                        //{
                        //    case 29:
                        //        index.Type = ANIMATION_GROUPS_TYPE.MONSTER;
                        //        break;
                        //    case 31: // what is this?
                        //        break;
                        //    case 32:
                        //        index.Type = ANIMATION_GROUPS_TYPE.EQUIPMENT;
                        //        break;
                        //    case 48:
                        //    case 68:
                        //        index.Type = ANIMATION_GROUPS_TYPE.HUMAN;
                        //        break;
                        //}

                        for (int k = 0; k < replaces; k++)
                        {
                            int oldIdx = _reader.ReadInt();
                            uint frameCount = _reader.ReadUInt();
                            int newIDX = _reader.ReadInt();

                            //sb.AppendLine($"\t\told: {oldIdx}\t\tframecount: {frameCount}\t\tnew: {newIDX}");

                            if (frameCount == 0)
                                index.Groups[oldIdx] = index.Groups[newIDX];
                            else
                            {
                                //int offset = 64;
                                if (animID == 0x02df)
                                {
                                }

                                //_reader.Skip(40);

                                //byte[] unk = new byte[20];

                                //for (int o = 0; o < 20; o++)
                                //{
                                //    unk[o] = _reader.ReadByte();
                                //}

                                //for (int j = 0; j < 5; j++)
                                //{
                                //    index.Groups[oldIdx].Direction[j].FrameCount = (byte) frameCount;
                                //}
                            }

                            _reader.Skip(60);
                        }

                        int toread = (int) (_reader.Length - _reader.Position);
                        byte[] data = _reader.ReadArray(toread);
                        _reader.SetData(data, toread);

                        if (animID == 0x02df)
                        {
                            //int len = entry.Length;
                            //byte[] decc = new byte[len];

                            //ZLib.Compress(decbuffer, ref decc);

                            // ZLib.Pack(decc, ref len, decbuffer, decLen);
                        }

                        //if (!Directory.Exists("files"))
                        //    Directory.CreateDirectory("files");

                        //using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine("files", $"file_0x{animID:X4}"))))
                        //{
                        //    writer.Write(data);
                        //}
                        //sb.AppendLine("Data len: " + toread);
                        //for (int ii = 0; ii < toread; ii++)
                        //{
                        //    sb.AppendLine($"\t\tbyte[{ii}]   {data[ii]}");
                        //}
                        uint unk0 = _reader.ReadUInt();
                        _reader.Skip(1);
                        uint unk1 = _reader.ReadUInt();
                        //uint unk2 = _reader.ReadUInt();
                        //uint unk3 = _reader.ReadUInt();
                        //file.WriteAsync(sb.ToString());
                    }
                }

                //file.Dispose();
                animSeq.Dispose();
            }
        }

        public static void UpdateAnimationTable(uint flags)
        {
            for (int i = 0; i < MAX_ANIMATIONS_DATA_INDEX_COUNT; i++)
            {
                for (int g = 0; g < 100; g++)
                {
                    for (int d = 0; d < 5; d++)
                    {
                        bool replace = DataIndex[i].Groups[g].Direction[d].FileIndex >= 3;

                        if (DataIndex[i].Groups[g].Direction[d].FileIndex == 1)
                            replace = (flags & 0x08) != 0;
                        else if (DataIndex[i].Groups[g].Direction[d].FileIndex == 2)
                            replace = (flags & 0x10) != 0;

                        if (replace)
                        {
                            DataIndex[i].Groups[g].Direction[d].Address = DataIndex[i].Groups[g].Direction[d].PatchedAddress;
                            DataIndex[i].Groups[g].Direction[d].Size = DataIndex[i].Groups[g].Direction[d].PatchedSize;
                        }
                        else
                        {
                            DataIndex[i].Groups[g].Direction[d].Address = DataIndex[i].Groups[g].Direction[d].BaseAddress;
                            DataIndex[i].Groups[g].Direction[d].Size = DataIndex[i].Groups[g].Direction[d].BaseSize;
                        }
                    }
                }
            }
        }

        public static void GetAnimDirection(ref byte dir, ref bool mirror)
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

        public static void GetSittingAnimDirection(ref byte dir, ref bool mirror, ref int x, ref int y)
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

        public static ANIMATION_GROUPS GetGroupIndex(ushort graphic)
        {
            if (graphic >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
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

        public static byte GetDieGroupIndex(ushort id, bool second)
        {
            switch (DataIndex[id].Type)
            {
                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    return (byte) (second ? LOW_ANIMATION_GROUP.LAG_DIE_2 : LOW_ANIMATION_GROUP.LAG_DIE_1);
                case ANIMATION_GROUPS_TYPE.MONSTER:
                case ANIMATION_GROUPS_TYPE.SEA_MONSTER:

                    return (byte) (second ? HIGHT_ANIMATION_GROUP.HAG_DIE_2 : HIGHT_ANIMATION_GROUP.HAG_DIE_1);
                case ANIMATION_GROUPS_TYPE.HUMAN:
                case ANIMATION_GROUPS_TYPE.EQUIPMENT:

                    return (byte) (second ? PEOPLE_ANIMATION_GROUP.PAG_DIE_2 : PEOPLE_ANIMATION_GROUP.PAG_DIE_1);
            }

            return 0;
        }

        public static bool AnimationExists(ushort graphic, byte group)
        {
            bool result = false;

            if (graphic < MAX_ANIMATIONS_DATA_INDEX_COUNT && group < 100)
            {
                AnimationDirection d = DataIndex[graphic].Groups[group].Direction[0];
                result = d.Address != 0 || d.IsUOP;
            }

            return result;
        }

        public static bool LoadDirectionGroup(ref AnimationDirection animDir)
        {
            if (animDir.IsUOP)
                return TryReadUOPAnimDimension(ref animDir);

            if (animDir.Address == 0)
                return false;
            UOFileMul file = _files[animDir.FileIndex];

            // long to int can loss data
            _reader.SetData(file.StartAddress + (int) animDir.Address, animDir.Size);
            ReadFramesPixelData(ref animDir);

            return true;
        }

        private static unsafe bool TryReadUOPAnimDimension(ref AnimationDirection animDirection)
        {
            ref AnimationGroup dataindex = ref DataIndex[AnimID].Groups[AnimGroup];
            UOPAnimationData animData = dataindex.UOPAnimData;

            if (animData.FileIndex == 0 && animData.CompressedLength == 0 && animData.DecompressedLength == 0 && animData.Offset == 0)
            {
                Log.Message(LogTypes.Warning, "uop animData is null");

                return false;
            }

            animDirection.LastAccessTime = CoreGame.Ticks;
            int decLen = (int) animData.DecompressedLength;
            UOFileUopAnimation file = _filesUop[animData.FileIndex];
            file.Seek(animData.Offset);
            byte[] buffer = file.ReadArray<byte>((int) animData.CompressedLength);
            byte[] decbuffer = new byte[decLen];
            ZLib.Decompress(buffer, 0, decbuffer, decLen);

            //if (ZLib.Unpack(decbuffer, ref decLen, buffer, buffer.Length) != ZLib.ZLibError.Okay)
            //{
            //    Log.Message(LogTypes.Error, "Error to decompress uop animation");
            //    return false;
            //}
            _reader.SetData(decbuffer, decLen);
            _reader.Skip(8);
            int dcsize = _reader.ReadInt();
            int animID = _reader.ReadInt();
            _reader.Skip(16);
            int frameCount = _reader.ReadInt();
            IntPtr dataStart = _reader.StartAddress + _reader.ReadInt();
            _reader.SetData(dataStart);
            List<UOPFrameData> pixelDataOffsets = new List<UOPFrameData>();

            for (int i = 0; i < frameCount; i++)
            {
                UOPFrameData data = new UOPFrameData
                {
                    DataStart = _reader.PositionAddress
                };
                _reader.Skip(2);
                data.FrameID = _reader.ReadShort();
                _reader.Skip(8);
                data.PixelDataOffset = _reader.ReadUInt();
                int vsize = pixelDataOffsets.Count;

                if (vsize + 1 < data.FrameID)
                {
                    while (vsize + 1 != data.FrameID)
                    {
                        pixelDataOffsets.Add(new UOPFrameData());
                        vsize++;
                    }
                }

                pixelDataOffsets.Add(data);
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
            animDirection.FrameCount = (byte) (pixelDataOffsets.Count / 5);
            int dirFrameStartIdx = animDirection.FrameCount * Direction;

            if (animDirection.Frames == null)
                animDirection.Frames = new AnimationFrameTexture[animDirection.FrameCount];

            for (int i = 0; i < animDirection.FrameCount; i++)
            {
                if (animDirection.Frames[i] != null && !animDirection.Frames[i].IsDisposed)
                    continue;
                UOPFrameData frameData = pixelDataOffsets[i + dirFrameStartIdx];

                if (frameData.DataStart == IntPtr.Zero)
                    continue;
                _reader.SetData(frameData.DataStart + (int) frameData.PixelDataOffset);
                ushort* palette = (ushort*) _reader.StartAddress;
                _reader.Skip(512);
                short imageCenterX = _reader.ReadShort();
                short imageCenterY = _reader.ReadShort();
                short imageWidth = _reader.ReadShort();
                short imageHeight = _reader.ReadShort();

                //animDirection.Frames[i] = new AnimationFrame() { CenterX = imageCenterX, CenterY = imageCenterY };

                if (imageWidth == 0 || imageHeight == 0)
                {
                    Log.Message(LogTypes.Warning, "frame size is null");

                    continue;
                }

                int textureSize = imageWidth * imageHeight;
                ushort[] pixels = new ushort[textureSize];
                uint header = _reader.ReadUInt();
                long pos = _reader.PositionAddress.ToInt64();
                long end = (_reader.StartAddress + (int) _reader.Length).ToInt64();

                while (header != 0x7FFF7FFF && pos < end)
                {
                    ushort runLength = (ushort) (header & 0x0FFF);
                    int x = (int) ((header >> 22) & 0x03FF);

                    if ((x & 0x0200) > 0)
                        x |= unchecked((int) 0xFFFFFE00);
                    int y = (int) ((header >> 12) & 0x3FF);

                    if ((y & 0x0200) > 0)
                        y |= unchecked((int) 0xFFFFFE00);
                    x += imageCenterX;
                    y += imageCenterY + imageHeight;
                    int block = y * imageWidth + x;

                    for (int k = 0; k < runLength; k++)
                    {
                        ushort val = palette[_reader.ReadByte()];

                        if (val > 0)
                            val |= 0x8000;
                        pixels[block++] = val;
                    }

                    header = _reader.ReadUInt();
                }

                int uniqueAnimationIndex = ((AnimID & 0xfff) << 20) + ((AnimGroup & 0x3f) << 12) + ((Direction & 0x0f) << 8);
                uniqueAnimationIndex += i & 0xFF;
                ref AnimationFrameTexture f = ref animDirection.Frames[i];

                if (f == null)
                    f = new AnimationFrameTexture(uniqueAnimationIndex, imageWidth, imageHeight);
                f.CenterX = imageCenterX;
                f.CenterY = imageCenterY;
                f.SetData(pixels);
                _picker.Set(uniqueAnimationIndex, imageWidth, imageHeight, pixels);
            }

            _usedTextures.Add(new ToRemoveInfo
            {
                AnimID = AnimID, Group = AnimGroup, Direction = Direction
            });

            return true;
        }

        private static unsafe void ReadFramesPixelData(ref AnimationDirection animDir)
        {
            animDir.LastAccessTime = CoreGame.Ticks;
            ushort* palette = (ushort*) _reader.StartAddress;
            _reader.Skip(512);
            IntPtr dataStart = _reader.PositionAddress;
            uint frameCount = _reader.ReadUInt();
            animDir.FrameCount = (byte) frameCount;
            uint* frameOffset = (uint*) _reader.PositionAddress;
            animDir.Frames = new AnimationFrameTexture[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                if (animDir.Frames[i] != null && !animDir.Frames[i].IsDisposed)
                    continue;
                _reader.SetData(dataStart + (int) frameOffset[i]);
                short imageCenterX = _reader.ReadShort();
                short imageCenterY = _reader.ReadShort();
                short imageWidth = _reader.ReadShort();
                short imageHeight = _reader.ReadShort();

                if (imageWidth == 0 || imageHeight == 0) continue;
                int wantSize = imageWidth * imageHeight;
                ushort[] pixels = new ushort[wantSize];
                uint header = _reader.ReadUInt();
                long pos = _reader.PositionAddress.ToInt64();
                long end = (_reader.StartAddress + (int) _reader.Length).ToInt64();

                while (header != 0x7FFF7FFF && pos < end)
                {
                    ushort runLength = (ushort) (header & 0x0FFF);
                    int x = (int) ((header >> 22) & 0x03FF);

                    if ((x & 0x0200) > 0)
                        x |= unchecked((int) 0xFFFFFE00);
                    int y = (int) ((header >> 12) & 0x3FF);

                    if ((y & 0x0200) > 0)
                        y |= unchecked((int) 0xFFFFFE00);
                    x += imageCenterX;
                    y += imageCenterY + imageHeight;
                    int block = y * imageWidth + x;

                    for (int k = 0; k < runLength; k++)
                    {
                        ushort val = palette[_reader.ReadByte()];

                        if (val > 0)
                            pixels[block] = (ushort) (0x8000 | val);
                        else
                            pixels[block] = 0;
                        block++;
                    }

                    header = _reader.ReadUInt();
                }

                int uniqueAnimationIndex = ((AnimID & 0xfff) << 20) + ((AnimGroup & 0x3f) << 12) + ((Direction & 0x0f) << 8);
                uniqueAnimationIndex += i & 0xFF;
                ref AnimationFrameTexture f = ref animDir.Frames[i];

                if (f == null)
                    f = new AnimationFrameTexture(uniqueAnimationIndex, imageWidth, imageHeight);
                f.CenterX = imageCenterX;
                f.CenterY = imageCenterY;
                f.SetData(pixels);
                _picker.Set(uniqueAnimationIndex, imageWidth, imageHeight, pixels);
            }

            _usedTextures.Add(new ToRemoveInfo
            {
                AnimID = AnimID, Group = AnimGroup, Direction = Direction
            });
        }

        public static bool Contains(int g, int x, int y, int extra = 0)
        {
            return _picker.Get(g, x, y, extra);
        }

        public static void GetAnimationDimensions(byte frameIndex, Graphic id, byte dir, byte animGroup, out int x, out int y, out int w, out int h)
        {
            if (id < MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                if (_animDimensionCache.TryGetValue(id, out Rectangle rect))
                {
                    x = rect.X;
                    y = rect.Y;
                    w = rect.Width;
                    h = rect.Height;

                    return;
                }

                if (dir < 5)
                {
                    ref AnimationDirection direction = ref DataIndex[id].Groups[animGroup].Direction[dir];
                    int fc = direction.FrameCount;

                    if (fc > 0)
                    {
                        if (frameIndex >= fc) frameIndex = 0;

                        if (direction.Frames != null)
                        {
                            ref AnimationFrameTexture animationFrameTexture = ref direction.Frames[frameIndex];
                            x = animationFrameTexture.CenterX;
                            y = animationFrameTexture.CenterY;
                            w = animationFrameTexture.Width;
                            h = animationFrameTexture.Height;
                            _animDimensionCache.Add(id, new Rectangle(x, y, w, h));

                            return;
                        }
                    }
                }

                ref AnimationDirection direction1 = ref DataIndex[id].Groups[animGroup].Direction[0];

                if (direction1.Address != 0)
                {
                    if (!direction1.IsVerdata)
                    {
                        UOFileMul file = _files[direction1.FileIndex];
                        _reader.SetData(file.StartAddress + (int) direction1.Address, direction1.Size);
                        ReadFrameDimensionData(frameIndex, out x, out y, out w, out h);
                        _animDimensionCache.Add(id, new Rectangle(x, y, w, h));

                        return;
                    }
                }
                else if (direction1.IsUOP)
                {
                    ref UOPAnimationData animDataStruct = ref DataIndex[AnimID].Groups[AnimGroup].UOPAnimData;

                    if (!(animDataStruct.FileIndex == 0 && animDataStruct.CompressedLength == 0 && animDataStruct.DecompressedLength == 0 && animDataStruct.Offset == 0))
                    {
                        int decLen = (int) animDataStruct.DecompressedLength;
                        UOFileUopAnimation file = _filesUop[animDataStruct.FileIndex];
                        file.Seek(animDataStruct.Offset);
                        byte[] buffer = file.ReadArray<byte>((int) animDataStruct.CompressedLength);
                        byte[] decbuffer = new byte[decLen];
                        ZLib.Decompress(buffer, 0, decbuffer, decLen);

                        //if (ZLib.Unpack(decbuffer, ref decLen, buffer, buffer.Length) == ZLib.ZLibError.Okay)
                        //{
                        _reader.SetData(decbuffer, decLen);
                        _reader.Skip(8);
                        int dcsize = _reader.ReadInt();
                        int animID = _reader.ReadInt();
                        _reader.Skip(16);
                        int frameCount = _reader.ReadInt();
                        IntPtr dataStart = _reader.StartAddress + _reader.ReadInt();
                        _reader.SetData(dataStart);

                        UOPFrameData data = new UOPFrameData
                        {
                            DataStart = _reader.PositionAddress
                        };
                        _reader.Skip(2);
                        data.FrameID = _reader.ReadShort();
                        _reader.Skip(8);
                        data.PixelDataOffset = _reader.ReadUInt();
                        _reader.SetData(data.DataStart + (int) data.PixelDataOffset);
                        _reader.Skip(512);
                        x = _reader.ReadShort();
                        y = _reader.ReadShort();
                        w = _reader.ReadShort();
                        h = _reader.ReadShort();
                        _animDimensionCache.Add(id, new Rectangle(x, y, w, h));

                        return;
                        //}
                    }
                }
            }

            x = 0;
            y = 0;
            w = 0;
            h = 0;
        }

        private static unsafe void ReadFrameDimensionData(byte frameIndex, out int x, out int y, out int w, out int h)
        {
            _reader.Skip(512);
            byte* dataStart = (byte*) _reader.PositionAddress;
            uint frameCount = _reader.ReadUInt();
            if (frameCount > 0 && frameIndex >= frameCount) frameIndex = 0;

            if (frameIndex < frameCount)
            {
                uint* frameOffset = (uint*) _reader.PositionAddress;
                _reader.SetData((IntPtr) (dataStart + frameOffset[frameIndex]));
                x = _reader.ReadShort();
                y = _reader.ReadShort();
                w = _reader.ReadShort();
                h = _reader.ReadShort();
            }
            else
                x = y = w = h = 0;
        }

        public static void ClearUnusedTextures()
        {
            int count = 0;
            long ticks = CoreGame.Ticks - 3000;

            for (int i = 0; i < _usedTextures.Count; i++)
            {
                ToRemoveInfo info = _usedTextures[i];
                ref AnimationDirection dir = ref DataIndex[info.AnimID].Groups[info.Group].Direction[info.Direction];

                if (dir.LastAccessTime < ticks)
                {
                    for (int j = 0; j < dir.FrameCount; j++)
                    {
                        if (dir.Frames[j] != null)
                        {
                            dir.Frames[j].Dispose();
                            dir.Frames[j] = null;
                        }
                    }

                    dir.FrameCount = 0;
                    dir.Frames = null;
                    dir.LastAccessTime = 0;
                    _usedTextures.RemoveAt(i--);

                    if (++count >= 5)
                        break;
                }
            }
        }

        private struct ToRemoveInfo
        {
            public int AnimID;
            public int Group;
            public int Direction;
        }

        private struct UOPFrameData
        {
            public IntPtr DataStart;
            public short FrameID;
            public uint PixelDataOffset;
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

    public class UOFileUopAnimation : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private readonly int _indexFile;

        public UOFileUopAnimation(string path, int index) : base(path)
        {
            _indexFile = index;
        }

        internal void LoadEx(ref Dictionary<ulong, UOPAnimationData> hashes)
        {
            Load();
            Seek(0);

            if (ReadInt() != UOP_MAGIC_NUMBER)
                throw new ArgumentException("Bad uop file");
            Skip(8);
            long nextblock = ReadLong();
            Skip(4);

            do
            {
                Seek(nextblock);
                int fileCount = ReadInt();
                nextblock = ReadLong();

                for (int i = 0; i < fileCount; i++)
                {
                    long offset = ReadLong();
                    int headerLength = ReadInt();
                    int compressedLength = ReadInt();
                    int decompressedLength = ReadInt();
                    ulong hash = ReadULong();
                    Skip(6);

                    if (offset == 0)
                        continue;
                    UOPAnimationData data = new UOPAnimationData((uint) (offset + headerLength), (uint) compressedLength, (uint) decompressedLength, _indexFile);
                    hashes.Add(hash, data);
                }
            } while (nextblock != 0);
        }
    }

    public struct IndexAnimation
    {
        public ushort Graphic { get; set; }

        public ushort Color { get; set; }

        public ANIMATION_GROUPS_TYPE Type { get; set; }

        public uint Flags { get; set; }

        public sbyte MountedHeightOffset { get; set; }

        public bool IsUOP { get; set; }

        // 100
        public AnimationGroup[] Groups { get; set; }
    }

    public struct AnimationGroup
    {
        // 5
        public AnimationDirection[] Direction;
        public UOPAnimationData UOPAnimData;

        public override string ToString()
        {
            return UOPAnimData.Offset != 0 ? "Has uop data" : Direction[0].Address != 0 ? "Has mul data" : "-";
        }
    }

    public struct AnimationDirection
    {
        public byte FrameCount;
        public long BaseAddress;
        public uint BaseSize;
        public long PatchedAddress;
        public uint PatchedSize;
        public int FileIndex;
        public long Address;
        public uint Size;
        public bool IsUOP;
        public bool IsVerdata;
        public long LastAccessTime;
        public AnimationFrameTexture[] Frames;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct AnimIdxBlock
    {
        public readonly uint Position;
        public readonly uint Size;
        public readonly uint Unknown;
    }

    public readonly struct EquipConvData
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

    public readonly struct UOPAnimationData
    {
        public UOPAnimationData(uint offset, uint clen, uint dlen, int index)
        {
            Offset = offset;
            CompressedLength = clen;
            DecompressedLength = dlen;
            FileIndex = index;
        }

        public readonly uint Offset;
        public readonly uint CompressedLength;
        public readonly uint DecompressedLength;
        public readonly int FileIndex;
    }
}