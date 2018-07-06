using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ClassicUO.AssetsLoader
{
    public static class Animations
    {
        private const int MAX_ANIMATIONS_DATA_INDEX_COUNT = 2048;

        private static readonly UOFileMul[] _files = new UOFileMul[5];
        private static readonly UOFileUopAnimation[] _filesUop = new UOFileUopAnimation[4];
        private static readonly IndexAnimation[] _dataIndex = new IndexAnimation[MAX_ANIMATIONS_DATA_INDEX_COUNT];
        private static readonly List<Tuple<ushort, byte>>[] _groupReplaces = new List<Tuple<ushort, byte>>[2]
        {
            new List<Tuple<ushort, byte>>(),
            new List<Tuple<ushort, byte>>()
        };
        private static readonly Dictionary<ushort, Dictionary<ushort, EquipConvData>> _equipConv = new Dictionary<ushort, Dictionary<ushort, EquipConvData>>();

        private static byte _animGroupCount = (int)PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;
        private static readonly DataReader _reader = new DataReader();


        public static ushort Color { get; set; }
        public static byte AnimGroup { get; set; }
        public static byte Direction { get; set; }
        public static ushort AnimID { get; set; }


        public static void Load()
        {
            Dictionary<ulong, UOPAnimationData> hashes = new Dictionary<ulong, UOPAnimationData>();

            for (int i = 0; i < 5; i++)
            {
                string pathmul = Path.Combine(FileManager.UoFolderPath, "anim" + (i == 0 ? "" : (i + 1).ToString()) + ".mul");
                string pathidx = Path.Combine(FileManager.UoFolderPath, "anim" + (i == 0 ? "" : (i + 1).ToString()) + ".idx");

                if (File.Exists(pathmul) && File.Exists(pathidx))
                {
                    _files[i] = new UOFileMul(pathmul, pathidx, 0, i == 0 ? 6 : 0);
                }

                if (i > 0 && FileManager.ClientVersion >= ClientVersions.CV_7000)
                {
                    string pathuop = Path.Combine(FileManager.UoFolderPath, string.Format("AnimationFrame{0}.uop", i));
                    if (File.Exists(pathuop))
                    {
                        _filesUop[i - 1] = new UOFileUopAnimation(pathuop, i - 1);
                        _filesUop[i - 1].LoadEx(ref hashes);
                    }
                }
            }

            if (FileManager.ClientVersion >= ClientVersions.CV_500A)
            {
                string[] typeNames = new string[5] { "monster", "sea_monster", "animal", "human", "equipment" };

                using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "mobtypes.txt"))))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line.Length <= 0 || line.Length < 3 || line[0] == '#')
                            continue;

                        string[] parts = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        int id = int.Parse(parts[0]);
                        if (id >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            continue;

                        string testType = parts[1].ToLower();

                        int commentIdx = parts[2].IndexOf('#');
                        if (commentIdx > 0)
                            parts[2] = parts[2].Substring(0, commentIdx - 1);
                        else if (commentIdx == 0)
                            continue;

                        uint number = uint.Parse(parts[2], System.Globalization.NumberStyles.HexNumber);
                        for (int i = 0; i < 5; i++)
                        {
                            if (testType == typeNames[i])
                            {
                                _dataIndex[id].Type = (ANIMATION_GROUPS_TYPE)i;
                                _dataIndex[id].Flags = (0x80000000 | number);
                                break;
                            }
                        }
                    }
                }
            }


            int animIdxBlockSize = Marshal.SizeOf<AnimIdxBlock>();

            var idxfile0 = _files[0].IdxFile;
            long maxAddress0 = (long)idxfile0.StartAddress + idxfile0.Length;
            var idxfile2 = _files[1].IdxFile;
            long maxAddress2 = (long)idxfile2.StartAddress + idxfile2.Length;
            var idxfile3 = _files[2].IdxFile;
            long maxAddress3 = (long)idxfile3.StartAddress + idxfile3.Length;
            var idxfile4 = _files[3].IdxFile;
            long maxAddress4 = (long)idxfile4.StartAddress + idxfile4.Length;
            var idxfile5 = _files[4].IdxFile;
            long maxAddress5 = (long)idxfile5.StartAddress + idxfile5.Length;

            for (int i = 0; i < MAX_ANIMATIONS_DATA_INDEX_COUNT; i++)
            {
                ANIMATION_GROUPS_TYPE groupTye = ANIMATION_GROUPS_TYPE.UNKNOWN;
                int findID = 0;

                if (i >= 200)
                {
                    if (i >= 400)
                    {
                        groupTye = ANIMATION_GROUPS_TYPE.HUMAN;
                        findID = (((i - 400) * 175) + 35000) * animIdxBlockSize;
                    }
                    else
                    {
                        groupTye = ANIMATION_GROUPS_TYPE.ANIMAL;
                        findID = (((i - 200) * 65) + 22000) * animIdxBlockSize;
                    }
                }
                else
                {
                    groupTye = ANIMATION_GROUPS_TYPE.MONSTER;
                    findID = (i * 110) * animIdxBlockSize;
                }

                _dataIndex[i].Graphic = (ushort)i;

                int count = 0;

                switch (groupTye)
                {
                    case ANIMATION_GROUPS_TYPE.MONSTER:
                    case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                        count = (int)HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT;
                        break;
                    case ANIMATION_GROUPS_TYPE.HUMAN:
                    case ANIMATION_GROUPS_TYPE.EQUIPMENT:
                        count = (int)PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;
                        break;
                    case ANIMATION_GROUPS_TYPE.ANIMAL:
                    default:
                        count = (int)LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;
                        break;
                }

                _dataIndex[i].Type = groupTye;

                IntPtr address = _files[0].IdxFile.StartAddress + findID;

                _dataIndex[i].Groups = new AnimationGroup[100];

                for (int j = 0; j < 100; j++)
                {
                    _dataIndex[i].Groups[j].Direction = new AnimationDirection[5];


                    if (j >= count)
                        continue;

                    int offset = j * 5;
                    for (int d = 0; d < 5; d++)
                    {
                        unsafe
                        {
                            AnimIdxBlock* aidx = (AnimIdxBlock*)(address + ((offset + d) * animIdxBlockSize));
                            if ((long)aidx >= maxAddress0)
                                break;

                            if (aidx->Size > 0 && aidx->Position != 0xFFFFFFFF &&
                                aidx->Size != 0xFFFFFFFF)
                            {
                                _dataIndex[i].Groups[j].Direction[d].BaseAddress = aidx->Position;
                                _dataIndex[i].Groups[j].Direction[d].BaseSize = aidx->Size;
                                _dataIndex[i].Groups[j].Direction[d].Address = _dataIndex[i].Groups[j].Direction[d].BaseAddress;
                                _dataIndex[i].Groups[j].Direction[d].Size = _dataIndex[i].Groups[j].Direction[d].BaseSize;
                            }

                        }
                        
                    }
                }
            }

            void readAnimDef(in StreamReader reader, in int idx)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length <= 0 || line[0] == '#')
                        continue;

                    string[] parts = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;

                    ushort group = ushort.Parse(parts[0]);

                    int first = parts[1].IndexOf("{");
                    int last = parts[1].IndexOf("}");

                    int replaceGroup = int.Parse(parts[1].Substring(first + 1, last - 1));
                
                    _groupReplaces[idx].Add(new Tuple<ushort, byte>(group, (byte)replaceGroup));
                }
            }

            using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "Anim1.def"))))
                readAnimDef(reader, 0);
            using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "Anim2.def"))))
                readAnimDef(reader, 1);


            if (FileManager.ClientVersion < ClientVersions.CV_305D)
                return;

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

                    int count = 0;
                    int[] ignoreGroups = { -1, -1 };

                    switch (_dataIndex[checkIndex].Type)
                    {
                        case ANIMATION_GROUPS_TYPE.MONSTER:
                        case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                            count = (int)HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT;
                            ignoreGroups[0] = (int)HIGHT_ANIMATION_GROUP.HAG_DIE_1;
                            ignoreGroups[1] = (int)HIGHT_ANIMATION_GROUP.HAG_DIE_2;
                            break;
                        case ANIMATION_GROUPS_TYPE.HUMAN:
                        case ANIMATION_GROUPS_TYPE.EQUIPMENT:
                            count = (int)PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;
                            ignoreGroups[0] = (int)PEOPLE_ANIMATION_GROUP.PAG_DIE_1;
                            ignoreGroups[1] = (int)PEOPLE_ANIMATION_GROUP.PAG_DIE_2;
                            break;
                        case ANIMATION_GROUPS_TYPE.ANIMAL:
                            count = (int)LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;
                            ignoreGroups[0] = (int)LOW_ANIMATION_GROUP.LAG_DIE_1;
                            ignoreGroups[1] = (int)LOW_ANIMATION_GROUP.LAG_DIE_2;
                            break;
                        default:
                            break;
                    }

                    for (int j = 0; j < count; j++)
                    {
                        if (j == ignoreGroups[0] || j == ignoreGroups[1])
                            continue;

                        for (int d = 0; d < 5; d++)
                        {
                            _dataIndex[index].Groups[j].Direction[d].BaseAddress = _dataIndex[checkIndex].Groups[j].Direction[d].BaseAddress;
                            _dataIndex[index].Groups[j].Direction[d].BaseSize = _dataIndex[checkIndex].Groups[j].Direction[d].BaseSize;
                            _dataIndex[index].Groups[j].Direction[d].Address = _dataIndex[index].Groups[j].Direction[d].BaseAddress;
                            _dataIndex[index].Groups[j].Direction[d].Size = _dataIndex[index].Groups[j].Direction[d].BaseSize;

                            if (_dataIndex[index].Groups[j].Direction[d].PatchedAddress <= 0)
                            {
                                _dataIndex[index].Groups[j].Direction[d].PatchedAddress = _dataIndex[checkIndex].Groups[j].Direction[d].PatchedAddress;
                                _dataIndex[index].Groups[j].Direction[d].PatchedSize = _dataIndex[checkIndex].Groups[j].Direction[d].PatchedSize;
                                _dataIndex[index].Groups[j].Direction[d].FileIndex = _dataIndex[checkIndex].Groups[j].Direction[d].FileIndex;
                            }

                            if (_dataIndex[index].Groups[j].Direction[d].BaseAddress <= 0)
                            {
                                _dataIndex[index].Groups[j].Direction[d].BaseAddress = _dataIndex[index].Groups[j].Direction[d].PatchedAddress;
                                _dataIndex[index].Groups[j].Direction[d].BaseSize = _dataIndex[index].Groups[j].Direction[d].PatchedSize;
                                _dataIndex[index].Groups[j].Direction[d].Address = _dataIndex[index].Groups[j].Direction[d].BaseAddress;
                                _dataIndex[index].Groups[j].Direction[d].Size = _dataIndex[index].Groups[j].Direction[d].BaseSize;
                            }
                        }
                    }

                    _dataIndex[index].Type = _dataIndex[checkIndex].Type;
                    _dataIndex[index].Flags = _dataIndex[checkIndex].Flags;
                    _dataIndex[index].Graphic = checkIndex;
                    _dataIndex[index].Color = ushort.Parse(part2);         
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

                    string[] parts = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        ushort index = ushort.Parse(parts[0]);
                        if (index >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            continue;
                        int[] anim = { int.Parse(parts[1]), -1, -1, -1 };
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

                        if (anim[0] != -1 && maxAddress2 != 0)
                        {
                            animFile = 1;
                            realAnimID = (ushort)anim[0];

                            if (realAnimID == 68)
                                realAnimID = 122;

                            if (realAnimID >= 200)
                            {
                                startAnimID = ((realAnimID - 200) * 65) + 22000;
                                groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                            }
                            else
                            {
                                startAnimID = realAnimID * 110;
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                        }
                        else if (anim[1] != -1 && maxAddress3 != 0)
                        {
                            animFile = 2;
                            realAnimID = (ushort)anim[1];

                            if (realAnimID >= 200)
                            {
                                if (realAnimID >= 400)
                                {
                                    startAnimID = ((realAnimID - 400) * 175) + 35000;
                                    groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                                }
                                else
                                {
                                    startAnimID = ((realAnimID - 200) * 110) + 22000;
                                    groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                                }
                            }
                            else
                            {
                                startAnimID = (realAnimID * 65) + 9000;
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                        }
                        else if (anim[2] != -1 && maxAddress4 != 0)
                        {
                            animFile = 3;
                            realAnimID = (ushort)anim[2];

                            if (realAnimID >= 200)
                            {
                                if (realAnimID >= 400)
                                {
                                    startAnimID = ((realAnimID - 400) * 175) + 35000;
                                    groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                                }
                                else
                                {
                                    startAnimID = ((realAnimID - 200) * 65) + 22000;
                                    groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                                }
                            }
                            else
                            {
                                startAnimID = realAnimID * 110;
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                        }
                        else if (anim[3] != -1 && maxAddress5 != 0)
                        {
                            animFile = 4;
                            realAnimID = (ushort)anim[3];
                            mountedHeightOffset = -9;

                            if (realAnimID == 34)
                                startAnimID = ((realAnimID - 200) * 65) + 22000;
                            else if (realAnimID >= 200)
                            {
                                if (realAnimID >= 400)
                                {
                                    startAnimID = ((realAnimID - 400) * 175) + 35000;
                                    groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                                }
                                else
                                {
                                    startAnimID = ((realAnimID - 200) * 65) + 22000;
                                    groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                                }
                            }
                            else
                            {
                                startAnimID = realAnimID * 110;
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                        }

                        if ( startAnimID != -1)
                        {
                            startAnimID = startAnimID * animIdxBlockSize;

                            var currentIdxFile = _files[animFile].IdxFile;

                            if ((uint)startAnimID < currentIdxFile.Length)
                            {
                                _dataIndex[index].MountedHeightOffset = mountedHeightOffset;

                                if (FileManager.ClientVersion < ClientVersions.CV_500A || groupType == ANIMATION_GROUPS_TYPE.UNKNOWN)
                                {
                                    if (realAnimID >= 200)
                                    {
                                        if (realAnimID >= 400)
                                            _dataIndex[index].Type = ANIMATION_GROUPS_TYPE.HUMAN;
                                        else
                                            _dataIndex[index].Type = ANIMATION_GROUPS_TYPE.ANIMAL;
                                    }
                                    else
                                        _dataIndex[index].Type = ANIMATION_GROUPS_TYPE.MONSTER;
                                }
                                else if (groupType != ANIMATION_GROUPS_TYPE.UNKNOWN)
                                    _dataIndex[index].Type = groupType;

                                int count = 0;

                                switch (_dataIndex[index].Type)
                                {
                                    case ANIMATION_GROUPS_TYPE.MONSTER:
                                    case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                                        count = (int)HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT;
                                        break;
                                    case ANIMATION_GROUPS_TYPE.HUMAN:
                                    case ANIMATION_GROUPS_TYPE.EQUIPMENT:
                                        count = (int)PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;
                                        break;
                                    case ANIMATION_GROUPS_TYPE.ANIMAL:
                                    default:
                                        count = (int)LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT;
                                        break;
                                }


                                IntPtr address = currentIdxFile.StartAddress + startAnimID;
                                IntPtr maxaddress = currentIdxFile.StartAddress + (int)currentIdxFile.Length;

                                for (int j = 0; j < count; j++)
                                {
                                    int offset = j * 5;
                                    for (int d = 0; d < 5; d++)
                                    {
                                        unsafe
                                        {
                                            AnimIdxBlock* aidx = (AnimIdxBlock*)(address + ((offset + d) * animIdxBlockSize));
                                            if ((long)aidx >= (long)maxaddress)
                                                break;

                                            if (aidx->Size > 0 && aidx->Position != 0xFFFFFFFF &&
                                                aidx->Size != 0xFFFFFFFF)
                                            {
                                                _dataIndex[index].Groups[j].Direction[d].PatchedAddress = aidx->Position;
                                                _dataIndex[index].Groups[j].Direction[d].PatchedSize = aidx->Size;
                                                _dataIndex[index].Groups[j].Direction[d].FileIndex = animFile;
                                            }

                                        }
                                    }
                                }

                            }
                        }
                    }
                   
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

                    string[] parts = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

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

                    int[] ignoreGroups = { -1, -1 };

                    switch (_dataIndex[checkIndex].Type)
                    {
                        case ANIMATION_GROUPS_TYPE.MONSTER:
                        case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                            ignoreGroups[0] = (int)HIGHT_ANIMATION_GROUP.HAG_DIE_1;
                            ignoreGroups[1] = (int)HIGHT_ANIMATION_GROUP.HAG_DIE_2;
                            break;
                        case ANIMATION_GROUPS_TYPE.HUMAN:
                        case ANIMATION_GROUPS_TYPE.EQUIPMENT:
                            ignoreGroups[0] = (int)PEOPLE_ANIMATION_GROUP.PAG_DIE_1;
                            ignoreGroups[1] = (int)PEOPLE_ANIMATION_GROUP.PAG_DIE_2;
                            break;
                        case ANIMATION_GROUPS_TYPE.ANIMAL:
                            ignoreGroups[0] = (int)LOW_ANIMATION_GROUP.LAG_DIE_1;
                            ignoreGroups[1] = (int)LOW_ANIMATION_GROUP.LAG_DIE_2;
                            break;
                        default:
                            break;
                    }

                    if (ignoreGroups[0] == -1)
                        continue;

                    for (int j = 0; j < 2; j++)
                    {
                        for (int d = 0; d < 5; d++)
                        {
                            _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress = _dataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].BaseAddress;
                            _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseSize = _dataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].BaseSize;
                            _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].Address = _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress;
                            _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].Size = _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseSize;

                            if (_dataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedAddress <= 0)
                            {
                                _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedAddress = _dataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].PatchedAddress;
                                _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedSize = _dataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].PatchedSize;
                                _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].FileIndex = _dataIndex[checkIndex].Groups[ignoreGroups[j]].Direction[d].FileIndex;
                            }

                            if (_dataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress <= 0)
                            {
                                _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress = _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedAddress;
                                _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseSize = _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].PatchedSize;
                                _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].Address = _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseAddress;
                                _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].Size = _dataIndex[index].Groups[ignoreGroups[j]].Direction[d].BaseSize;
                            }
                        }
                    }

                    _dataIndex[index].Type = _dataIndex[checkIndex].Type;
                    _dataIndex[index].Flags = _dataIndex[checkIndex].Flags;
                    _dataIndex[index].Graphic = checkIndex;
                    _dataIndex[index].Color = ushort.Parse(part2);
                }
            }

            using (StreamReader reader = new StreamReader(File.OpenRead(Path.Combine(FileManager.UoFolderPath, "Equipconv.def"))))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length <= 0 || line[0] == '#' || !char.IsNumber(line[0]))
                        continue;

                    string[] parts = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5)
                    {
                        ushort body = (ushort)int.Parse(parts[0]);
                        if (body >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            continue;
                        ushort graphic = (ushort)int.Parse(parts[1]);
                        if (graphic >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            continue;
                        ushort newgraphic = (ushort)int.Parse(parts[2]);
                        if (newgraphic >= MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            newgraphic = graphic;

                        ushort gump = (ushort)int.Parse(parts[3]);
                        if (gump > ushort.MaxValue)
                            continue;
                        else if (gump == 0)
                            gump = graphic;
                        else if (gump == 0xFFFF)
                            gump = newgraphic;
                        ushort color = (ushort)int.Parse(parts[4]);

                        if (!_equipConv.TryGetValue(body, out var dict))
                        {
                            _equipConv.Add(body, new Dictionary<ushort, EquipConvData>());

                            if (!_equipConv.TryGetValue(body, out dict))
                                continue;
                        }

                        dict.Add(graphic, new EquipConvData(newgraphic, gump, color));
                    }

                }
            }



            byte maxGroup = 0;

            for (int animID = 0; animID < MAX_ANIMATIONS_DATA_INDEX_COUNT; animID++)
            {
                for (byte grpID = 0; grpID < 100; grpID++)
                {
                    string hashstring = string.Format("build/animationlegacyframe/{0:D6}/{1:D2}.bin", animID, grpID);
                    ulong hash = UOFileUop.CreateHash(hashstring);
                    if (hashes.TryGetValue(hash, out var data))
                    {
                        if (grpID > maxGroup)
                            maxGroup = grpID;

                        _dataIndex[animID].IsUOP = true;
                        _dataIndex[animID].Groups[grpID].UOPAnimData = data;

                        for (byte dirID = 0; dirID < 5; dirID++)
                        {
                            _dataIndex[animID].Groups[grpID].Direction[dirID].IsUOP = true;
                            _dataIndex[animID].Groups[grpID].Direction[dirID].BaseAddress = 0;
                            _dataIndex[animID].Groups[grpID].Direction[dirID].Address = 0;
                        }

                    }
                }
            }

            if (_animGroupCount < maxGroup)
                _animGroupCount = maxGroup;
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
                default:
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
                default:
                    break;
            }
        }



        public static bool LoadDirectionGroup(ref AnimationDirection animDir)
        {
            if (animDir.IsUOP)
                return TryReadUOPAnimDimension(ref animDir);
            if (animDir.Address == 0)
                return false;

            var file = _files[animDir.FileIndex];
            //byte[] animData = file.ReadArray<byte>(animDir.Address, (int)animDir.Size);

            // long to int can loss data
            _reader.SetData(file.StartAddress + (int)animDir.Address, animDir.Size);

            ReadFramesPixelData(ref animDir);
            return true;
        }

        private static unsafe bool TryReadUOPAnimDimension(ref AnimationDirection animDirection)
        {
            UOPAnimationData animData = _dataIndex[AnimID].Groups[AnimGroup].UOPAnimData;
            if (animData.FileIndex == 0 && animData.CompressedLength == 0 && animData.DecompressedLength == 0 && animData.Offset == 0)
            {
                Log.Message(LogTypes.Warning, "uop animData is null");
                return false;
            }

            int decLen = (int)animData.DecompressedLength;
            UOFileUopAnimation file = _filesUop[animData.FileIndex];
            file.Seek(animData.Offset);
            byte[] buffer = file.ReadArray<byte>((int)animData.CompressedLength);
            byte[] decbuffer = new byte[decLen];

            if (!Zlib.Decompress(buffer, 0, decbuffer, decLen))
            {
                Log.Message(LogTypes.Error, "Error to decompress uop animation");
                return false;
            }

            _reader.SetData(decbuffer, decLen);

            _reader.Skip(8);
            int dcsize = _reader.ReadInt();
            int animID = _reader.ReadInt();
            _reader.Skip(4 + 4 + 2 + 2 + 4);
            int frameCount = _reader.ReadInt();
            IntPtr dataStart = _reader.StartAddress + _reader.ReadInt();
            _reader.SetData(dataStart);
            List<UOPFrameData> pixelDataOffsets = new List<UOPFrameData>();

            for (int i = 0; i < frameCount; i++)
            {
                UOPFrameData data = new UOPFrameData()
                {
                    DataStart = (byte*)_reader.StartAddress
                };

                _reader.Skip(2);
                data.FrameID = _reader.ReadShort();
                _reader.Skip(8);
                data.PixelDataOffset = _reader.ReadUInt();
                int vsize = pixelDataOffsets.Count;
                if (vsize + 1 != data.FrameID)
                {
                    while (vsize + 1 != data.FrameID)
                    {
                        pixelDataOffsets.Add(new UOPFrameData());
                        vsize++;
                    }
                }
                pixelDataOffsets.Add(data);
            }

            int vectorSize = pixelDataOffsets.Count;
            if (vectorSize < 50)
            {
                while (vectorSize < 50)
                {
                    pixelDataOffsets.Add(new UOPFrameData());
                    vectorSize++;
                }
            }

            animDirection.FrameCount = (byte)(pixelDataOffsets.Count / 5);
            int dirFrameStartIdx = animDirection.FrameCount * Direction;
            if (animDirection.Frames == null)
                animDirection.Frames = new AnimationFrame[animDirection.FrameCount];

            for (int i = 0; i < animDirection.FrameCount; i++)
            {
                UOPFrameData frameData = pixelDataOffsets[i + dirFrameStartIdx];
                if (frameData.DataStart == null)
                    continue;

                _reader.SetData((IntPtr)frameData.DataStart + (int)frameData.PixelDataOffset);
                ushort* palette = (ushort*)_reader.StartAddress;
                _reader.Skip(512);

                short imageCenterX = _reader.ReadShort();
                short imageCenterY = _reader.ReadShort();
                short imageWidth = _reader.ReadShort();
                short imageHeight = _reader.ReadShort();

                animDirection.Frames[i].CenterX = imageCenterX;
                animDirection.Frames[i].CenterY = imageCenterY;

                if (imageWidth <= 0 || imageHeight <= 0)
                {
                    Log.Message(LogTypes.Warning, "frame size is null");
                    continue;
                }

                int textureSize = imageWidth * imageHeight;
                ushort[] pixels = new ushort[textureSize];

                uint header = _reader.ReadUInt();

                long pos = _reader.PositionAddress.ToInt64();
                long end = (_reader.StartAddress + (int)_reader.Length).ToInt64();

                while (header != 0x7FFF7FFF && pos < end)
                {
                    ushort runLength = (ushort)(header & 0x0FFF);

                    int x = (int)((header >> 22) & 0x03FF);
                    if ((x & 0x0200) > 0)
                        x |= unchecked((int)0xFFFFFE00);

                    int y = (int)((header >> 12) & 0x3FF);
                    if ((y & 0x0200) > 0)
                        y |= unchecked((int)0xFFFFFE00);

                    x += imageCenterX;
                    y += imageCenterY + imageHeight;

                    int block = (y * imageWidth) + x;
                    for (int k = 0; k < runLength; k++)
                    {
                        ushort val = palette[_reader.ReadByte()];
                        if (val > 0)
                            val |= 0x8000;
                        pixels[block++] = val;
                    }

                    header = _reader.ReadUInt();
                }
            }


            //fixed (byte* ptrBuff = decbuffer)
            //{
            //    int count = 8;
            //    int dcsize = ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24);
            //    int id = ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24);

            //    count += 16;

            //    int frameCount = ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24);
            //    byte* dataStart = ptrBuff + (ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24));

            //    byte* ptr = dataStart;

            //    for (int i = 0; i < frameCount; i++)
            //    {
            //        UOPFrameData frameData = new UOPFrameData()
            //        {
            //            DataStart = ptr
            //        };

            //        count += 2;
            //        frameData.FrameID = (short)(ptrBuff[count++] | (ptrBuff[count++] << 8));
            //        count += 8;
            //        frameData.PixelDataOffset = (uint)(ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24));

            //        int vsize = pixelDataOffsets.Count;

            //        if (vsize + 1 != frameData.FrameID)
            //        {
            //            while (vsize + 1 != frameData.FrameID)
            //            {
            //                pixelDataOffsets.Add(new UOPFrameData());
            //                vsize++;
            //            }
            //        }
            //        pixelDataOffsets.Add(frameData);
            //    }

            //    int vectorSize = pixelDataOffsets.Count;
            //    if (vectorSize < 50)
            //    {
            //        while (vectorSize != 50)
            //        {
            //            pixelDataOffsets.Add(new UOPFrameData());
            //            vectorSize++;
            //        }
            //    }


            //    animDirection.FrameCount = (byte)(pixelDataOffsets.Count / 5);
            //    int dirFrameStartIdx = animDirection.FrameCount * Direction;

            //    if (animDirection.Frames == null)
            //        animDirection.Frames = new AnimationFrame[animDirection.FrameCount];

            //    for (int i = 0; i < animDirection.FrameCount; i++)
            //    {
            //        int currIdx = i + dirFrameStartIdx;

            //        if (pixelDataOffsets[currIdx].DataStart == null)
            //            continue;

            //        ptr = pixelDataOffsets[currIdx].DataStart + pixelDataOffsets[currIdx].PixelDataOffset;
            //        ushort* palette = (ushort*)ptr;
            //        count += 512;

            //        short imageCenterX = (short)(ptrBuff[count++] | (ptrBuff[count++] << 8));
            //        short imageCenterY = (short)(ptrBuff[count++] | (ptrBuff[count++] << 8));
            //        short imageWidth = (short)(ptrBuff[count++] | (ptrBuff[count++] << 8));
            //        short imageHeight = (short)(ptrBuff[count++] | (ptrBuff[count++] << 8));

            //        animDirection.Frames[i].CenterX = imageCenterX;
            //        animDirection.Frames[i].CenterY = imageCenterY;
            //        if (imageWidth <= 0 || imageHeight <= 0)
            //            continue;
            //        int textureSize = imageWidth * imageHeight;

            //        ushort[] data = new ushort[textureSize];

            //        uint header = (uint)(ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24));

            //        if (header != 0x7FFF7FFF && count < file.Length)
            //        {
            //            ushort runLength = (ushort)(header & 0x0FFF);
            //            int x = (int)((header >> 22) & 0x03FF);

            //            if ((x & 0x0200) > 0)
            //                x |= unchecked((int)0xFFFFFE00);

            //            int y = (int)((header >> 12) & 0x03FF);

            //            if ((y & 0x0200) > 0)
            //                y |= unchecked((int)0xFFFFFE00);

            //            x += imageCenterX;
            //            y += imageCenterY + imageHeight;

            //            int block = (y * imageWidth) + x;

            //            for (int k = 0; k < runLength; k++)
            //            {
            //                ushort val = palette[ptrBuff[count++]];
            //                if (val > 0)
            //                    val |= 0x8000;
            //                data[block++] = val;
            //            }

            //            header = (uint)(ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24));
            //        }
            //    }

            //}
            return true;
        }

        private static unsafe void ReadFramesPixelData(ref AnimationDirection animDir)
        {
            ushort* palette = (ushort*)_reader.StartAddress;
            _reader.Skip(512);
            IntPtr dataStart = _reader.PositionAddress;

            uint frameCount = _reader.ReadUInt();
            animDir.FrameCount = (byte)frameCount;

            uint* frameOffset = (uint*)_reader.PositionAddress;
            animDir.Frames = new AnimationFrame[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                _reader.SetData(dataStart + (int)frameOffset[i]);

                short imageCenterX = _reader.ReadShort();
                animDir.Frames[i].CenterX = imageCenterX;

                short imageCenterY = _reader.ReadShort();
                animDir.Frames[i].CenterY = imageCenterY;

                short imageWidth = _reader.ReadShort();
                short imageHeight = _reader.ReadShort();

                if (imageWidth <= 0 || imageHeight <= 0)
                {
                    Log.Message(LogTypes.Warning, "mul frame size is null");
                    continue;
                }

                int wantSize = imageWidth * imageHeight;

                ushort[] pixels = new ushort[wantSize];

                uint header = _reader.ReadUInt();

                long pos = _reader.PositionAddress.ToInt64();
                long end = (_reader.StartAddress + (int)_reader.Length).ToInt64();

                while ( header != 0x7FFF7FFF && pos < end)
                {
                    ushort runLength = (ushort)(header & 0x0FFF);

                    int x = (int)((header >> 22) & 0x03FF);
                    if ((x & 0x0200) > 0)
                        x |= unchecked((int)0xFFFFFE00);

                    int y = (int)((header >> 12) & 0x3FF);
                    if ((y & 0x0200) > 0)
                        y |= unchecked((int)0xFFFFFE00);

                    x += imageCenterX;
                    y += imageCenterY + imageHeight;

                    int block = (y * imageWidth) + x;
                    for (int k = 0; k < runLength; k++)
                    {
                        ushort val = palette[_reader.ReadByte()];
                        if (val > 0)
                            pixels[block] =  (ushort)(0x8000 | val);
                        else
                            pixels[block] = 0;
                        block++;
                    }

                    header = _reader.ReadUInt();
                }
            }

            //fixed (byte* ptrR = data)
            //{
            //    byte* ptrBuff = ptrR;
            //    byte* end = ptrR + data.Length;

            //    ushort* palette = (ushort*)ptrBuff;
            //    int count = 512;

            //    byte* dataStart = ptrBuff + count;

            //    int frameCount = (ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24));
            //    animDir.FrameCount = (byte)frameCount;

            //    uint* frameOffset = (uint*)(ptrBuff + count);

            //    animDir.Frames = new AnimationFrame[frameCount];

            //    for (int i = 0; i < frameCount; i++)
            //    {
            //        ptrBuff = dataStart + frameOffset[i];
            //        count = 0;

            //        short imageCenterX = (short)(ptrBuff[count++] | (ptrBuff[count++] << 8));
            //        animDir.Frames[i].CenterX = imageCenterX;

            //        short imageCenterY = (short)(ptrBuff[count++] | (ptrBuff[count++] << 8));
            //        animDir.Frames[i].CenterY = imageCenterY;

            //        ushort imageWidth = (ushort)(ptrBuff[count++] | (ptrBuff[count++] << 8));
            //        ushort imageHeight= (ushort)(ptrBuff[count++] | (ptrBuff[count++] << 8));

            //        if (imageWidth <= 0 || imageHeight <= 0)
            //            continue;

            //        int wantSize = imageWidth * imageHeight;
            //        ushort[] pixels = new ushort[wantSize];

            //        uint header = (uint)(ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24));

            //        while (header != 0x7FFF7FFF && ptrBuff < end)
            //        {
            //            ushort runLength = (ushort)(header & 0x0FFF);

            //            int x = (int)((header >> 22) & 0x03FF);

            //            if ((x & 0x0200) > 0)
            //                x |= unchecked((int)0xFFFFFE00);

            //            int y = (int)((header >> 12) & 0x03FF);

            //            if ((y & 0x0200) > 0)
            //                y |= unchecked((int)0xFFFFFE00);

            //            x += imageCenterX;
            //            y += imageCenterY + imageHeight;

            //            int block = (y * imageWidth) + x;
            //            for (int k = 0; k < runLength; k++)
            //            {
            //                ushort val = palette[ptrBuff[count++]];
            //                if (val > 0)
            //                    pixels[block] = (ushort)(0x8000 | val);
            //                else
            //                    pixels[block] = 0;
            //                block++;
            //            }

            //            header = (uint)(ptrBuff[count++] | (ptrBuff[count++] << 8) | (ptrBuff[count++] << 16) | (ptrBuff[count++] << 24));
            //        }
            //    }
            //}
        }



        struct UOPFrameData
        {
            public unsafe byte* DataStart;
            public short FrameID;
            public uint PixelDataOffset;
        }

        //public static AnimationFrame[] GetAnimation(int body, int action, int direction, ref int hue)
        //{
        //    BodyDef.Translate(ref body, ref hue);
        //    int type = GraphicHelper.Convert(ref body);
            
        //    GetFileToRead(body, action, direction, type, out UOFile file, out int index);

        //    bool flip = direction > 4;

        //    if (file is UOFileUopAnimation uopfile)
        //    {
        //        uopfile.Seek(uopfile.Entries[index].Offset);
        //        return LoadAnimationUop(uopfile, body, direction);
        //    }
            
        //    file.Seek(file.Entries[index].Offset);
        //    return LoadAnimation(file);
        //}

        //private static AnimationFrame[] LoadAnimation(UOFile file)
        //{
        //    ushort[] palette = new ushort[0x100];
        //    for (int i = 0; i < palette.Length; i++)
        //        palette[i] = (ushort)(file.ReadUShort() ^ 0x8000);

        //    int start = (int)file.Position;
        //    int frameCount = file.ReadInt();

        //    int[] lookups = new int[frameCount];
        //    for (int i = 0; i < lookups.Length; i++)
        //        lookups[i] = start + file.ReadInt();

        //    AnimationFrame[] frames = new AnimationFrame[frameCount];
        //    for (int i = 0; i < frames.Length; i++)
        //    {
        //        file.Seek(lookups[i]);
        //        frames[i] = new AnimationFrame(palette, file);
        //    }
        //    return frames;
        //}

        //private struct UopDataFrame
        //{
        //    public short ID;
        //    public int Offset;
        //    public int Start;

        //    public static UopDataFrame Null = new UopDataFrame()
        //    {
        //        ID = 0,
        //        Offset = 0,
        //        Start = -1,
        //    };
        //}

        //private static unsafe AnimationFrame[] LoadAnimationUop(UOFileUopAnimation file, int body, int direction)
        //{
        //    int start = 0;

        //    file.Uncompress(body);

        //    file.Skip(8);
        //    int dcsize = file.ReadInt();
        //    int animid = file.ReadInt();
        //    file.Skip(16);
        //    int framecount = file.ReadInt();
        //    int datastart = start + file.ReadInt();
        //    file.Seek(datastart);

        //    List<UopDataFrame> datas = new List<UopDataFrame>();
        //    for (int i = 0; i < framecount; i++)
        //    {
        //        UopDataFrame data = new UopDataFrame()
        //        {
        //            Start = (int)file.Position,
        //        };
        //        file.Skip(2);
        //        data.ID = file.ReadShort();
        //        file.Skip(8);
        //        data.Offset = file.ReadInt();

        //        int vsize = datas.Count;
        //        if (vsize + 1 != data.ID)
        //        {
        //            while (vsize + 1 != data.ID)
        //            {
        //                datas.Add(UopDataFrame.Null);
        //                vsize++;
        //            }
        //        }
        //        datas.Add(data);
        //    }

        //    int animframecount = datas.Count / 5;

        //    AnimationFrame[] frames = new AnimationFrame[animframecount];

        //    int dir = direction & 7;
        //    if (dir > 4)
        //        dir = dir - (dir - 4) * 2;

        //    int framestartidx = animframecount * dir;

        //    for (int i = 0; i < animframecount; i++)
        //    {
        //        UopDataFrame data = datas[i + framestartidx];
        //        if (data.Start == -1)
        //        {
        //            frames[i] = AnimationFrame.Null;
        //            continue;
        //        }

        //        file.Seek(data.Start + data.Offset);

        //        ushort[] palette = new ushort[0x100];
        //        for (int a = 0; a < palette.Length; a++)
        //            palette[a] = (ushort)(file.ReadUShort() ^ 0x8000);

        //        frames[i] = new AnimationFrame(palette, file);
        //    }

        //    return frames;
        //}

        //private static void GetFileToRead(int body, int action, int direction, int type, out UOFile file, out int index)
        //{
        //    switch (type)
        //    {
        //        default:
        //        case 1:
        //            if (body < 200)
        //                index = body * 110;
        //            else if (body < 400)
        //                index = 22000 + ((body - 200) * 65);
        //            else
        //                index = 35000 + ((body - 400) * 175);

        //            if (index >= _files[0].Entries.Length || (body < _files[5].Entries.Length && _files[5].Entries[body].IsUOP ))
        //            {
        //                file = _files[5];
        //                index = file.Entries[body].AnimID;
        //            }
        //            else
        //                file = _files[0];                    
        //            break;
        //        case 2:
        //            if (body < 200)
        //                index = body * 110;
        //            else
        //                index = 22000 + ((body - 200) * 65);

        //            if (index >= _files[1].Entries.Length || (body < _files[6].Entries.Length && _files[6].Entries[body].IsUOP))
        //            {
        //                file = _files[6];
        //                index = file.Entries[body].AnimID;
        //            }
        //            else
        //                file = _files[1];

        //            break;
        //        case 3:
        //            if (body < 300)
        //                index = body * 65;
        //            else if (body < 400)
        //                index = 33000 + ((body - 300) * 110);
        //            else
        //                index = 35000 + ((body - 400) * 175);

        //            if (index >= _files[2].Entries.Length || (index < _files[7].Entries.Length && _files[7].Entries[body].IsUOP))
        //            {
        //                file = _files[7];
        //                index = file.Entries[body].AnimID;
        //            }
        //            else
        //                file = _files[2];

        //            break;
        //        case 4:
        //            if (body < 200)
        //                index = body * 110;
        //            else if (body < 400)
        //                index = 22000 + ((body - 200) * 65);
        //            else
        //                index = 35000 + ((body - 400) * 175);

        //            if (index >= _files[3].Entries.Length || (body < _files[8].Entries.Length && _files[8].Entries[body].IsUOP))
        //            {
        //                file = _files[8];
        //                index = file.Entries[body].AnimID;
        //            }
        //            else
        //                file = _files[3];

        //            break;
        //        case 5:
        //            // NB: maybe wrong .uop
        //            file = _files[4];
        //            if ((body < 200) && (body != 34)) // looks strange, though it works.
        //                index = body * 110;
        //            else if (body < 400)
        //                index = 22000 + ((body - 200) * 65);
        //            else
        //                index = 35000 + ((body - 400) * 175);
        //            break;
        //    }

        //    index += action * 5;

        //    if (direction <= 4)
        //        index += direction;
        //    else
        //        index += direction - (direction - 4) * 2;
        //}

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


    //public class AnimationFrame
    //{
    //    public static readonly AnimationFrame Null = new AnimationFrame();
    //    public static readonly AnimationFrame[] Empty = { Null };

    //    const int DOUBLE_XOR = (0x200 << 22) | (0x200 << 12);
    //    const int END_OF_FRAME = 0x7FFF7FFF;

    //    private AnimationFrame()
    //    {
    //        CenterX = 0;
    //        CenterY = 0;
    //    }

    //    public unsafe AnimationFrame(ushort[] palette, UOFile file)
    //    {
    //        int centerX = file.ReadShort();
    //        int centerY = file.ReadShort();
    //        int width = file.ReadUShort();
    //        int height = file.ReadUShort();

    //        if (width == 0 || height == 0)
    //            return;

    //        // sittings ?

    //        ushort[] data = new ushort[width * height];

    //        fixed (ushort* pdata = data)
    //        {
    //            ushort* dataRef = pdata;

    //            int header;

    //            while ((header = file.ReadInt()) != END_OF_FRAME)
    //            {
    //                header ^= DOUBLE_XOR;

    //                int x = ((header >> 22) & 0x3FF) + centerX - 0x200;
    //                int y = ((header >> 12) & 0x3FF) + centerY + height - 0x200;

    //                ushort* cur = dataRef + y * width + x;
    //                ushort* end = cur + (header & 0xFFF);
    //                int filecount = 0;
    //                byte[] filedata = file.ReadArray<byte>(header & 0xFFF);
    //                while (cur < end)
    //                    *cur++ = palette[filedata[filecount++]];
    //            }

    //        }

    //        CenterX = centerX;
    //        CenterY = centerY;
    //        Data = data;
    //    }

    //    public int CenterX { get; }
    //    public int CenterY { get; }
    //    public ushort[] Data { get; }
    //}

    public class UOFileUopAnimation : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private int _indexFile;

        public UOFileUopAnimation(string path, int index) : base(path)
        {
            _indexFile = index;
        }

        public unsafe void Uncompress()
        {
            /*var e = Entries[index];
            Seek(e.Offset);*/
            //(int length, int extra, bool patcher) = SeekByEntryIndex(index);
            //byte[] buffer = ReadArray<byte>(length);
            //int clen = length;
            //int dlen = Entries[index].DecompressedLength;

            //byte[] decbuffer = new byte[dlen];
            //using (MemoryStream ms = new MemoryStream(buffer))
            //{
            //    ms.Seek(2, SeekOrigin.Begin);
            //    using (DeflateStream stream = new DeflateStream(ms, CompressionMode.Decompress))
            //    {
            //        for (int i = 0; i < dlen; i++)
            //            decbuffer[i] = (byte)stream.ReadByte();
            //    }
            //}

            //fixed (byte* ptr = decbuffer)
            //{
            //    _ptr = ptr;
            //    _position = 0;
            //    _length = decbuffer.Length;
            //}
        }


        internal void LoadEx(ref Dictionary<ulong, UOPAnimationData> hashes)
        {
            base.Load();

            Seek(0);
            if (ReadInt() != UOP_MAGIC_NUMBER)
                throw new ArgumentException("Bad uop file");

            Skip(8);
            long nextblock = ReadLong();
            Skip(4);

            Seek(nextblock);

            do
            {
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

                    UOPAnimationData data = new UOPAnimationData()
                    {
                        Offset = (uint)(offset + headerLength),
                        CompressedLength = (uint)compressedLength,
                        DecompressedLength = (uint)decompressedLength,
                        FileIndex = _indexFile,
                    };

                    hashes.Add(hash, data);
                }
                Seek(nextblock);
            } while (nextblock != 0);
        }

        //protected override void Load()
        //{
        //    base.Load();

        //    Seek(0);
        //    if (ReadInt() != UOP_MAGIC_NUMBER)
        //        throw new ArgumentException("Bad uop file");

        //    Skip(8);
        //    long nextblock = ReadLong();
        //    Skip(4);

        //    Seek(nextblock);

        //    do
        //    {
        //        int fileCount = ReadInt();
        //        nextblock = ReadLong();

        //        for (int i = 0; i < fileCount; i++)
        //        {
        //            long offset = ReadLong();
        //            int headerLength = ReadInt();
        //            int compressedLength = ReadInt();
        //            int decompressedLength = ReadInt();
        //            ulong hash = ReadULong();
        //            Skip(6);

        //            if (offset == 0)
        //                continue;

        //            UOPAnimationData data = new UOPAnimationData()
        //            {
        //                Offset = (uint)(offset + headerLength),
        //                CompressedLength = (uint)compressedLength,
        //                DecompressedLength = (uint)decompressedLength,
        //                FileIndex = _indexFile,
        //            };

        //            Hashes.Add(hash, data);
        //        }
        //        Seek(nextblock);
        //    } while (nextblock != 0);
        //}
    }

    public static class GraphicHelper
    {
        private static readonly int[][] Table = new int[4][];

        private static readonly int[][] _MountIDConv = 
        {
            new int[]{0x3E94, 0xF3}, // Hiryu
            new int[]{0x3E97, 0xC3}, // Beetle
            new int[]{0x3E98, 0xC2}, // Swamp Dragon
            new int[]{0x3E9A, 0xC1}, // Ridgeback
            new int[]{0x3E9B, 0xC0}, // Unicorn
            new int[]{0x3E9D, 0xC0}, // Unicorn
            new int[]{0x3E9C, 0xBF}, // Ki-Rin
            new int[]{0x3E9E, 0xBE}, // Fire Steed
            new int[]{0x3E9F, 0xC8}, // Horse
            new int[]{0x3EA0, 0xE2}, // Grey Horse
            new int[]{0x3EA1, 0xE4}, // Horse
            new int[]{0x3EA2, 0xCC}, // Brown Horse
            new int[]{0x3EA3, 0xD2}, // Zostrich
            new int[]{0x3EA4, 0xDA}, // Zostrich
            new int[]{0x3EA5, 0xDB}, // Zostrich
            new int[]{0x3EA6, 0xDC}, // Llama
            new int[]{0x3EA7, 0x74}, // Nightmare
            new int[]{0x3EA8, 0x75}, // Silver Steed
            new int[]{0x3EA9, 0x72}, // Nightmare
            new int[]{0x3EAA, 0x73}, // Ethereal Horse
            new int[]{0x3EAB, 0xAA}, // Ethereal Llama
            new int[]{0x3EAC, 0xAB}, // Ethereal Zostrich
            new int[]{0x3EAD, 0x84}, // Ki-Rin
            new int[]{0x3EAF, 0x78}, // Minax Warhorse
            new int[]{0x3EB0, 0x79}, // ShadowLords Warhorse
            new int[]{0x3EB1, 0x77}, // COM Warhorse
            new int[]{0x3EB2, 0x76}, // TrueBritannian Warhorse
            new int[]{0x3EB3, 0x90}, // Seahorse
            new int[]{0x3EB4, 0x7A}, // Unicorn
            new int[]{0x3EB5, 0xB1}, // Nightmare
            new int[]{0x3EB6, 0xB2}, // Nightmare
            new int[]{0x3EB7, 0xB3}, // Dark Nightmare
            new int[]{0x3EB8, 0xBC}, // Ridgeback
            new int[]{0x3EBA, 0xBB}, // Ridgeback
            new int[]{0x3EBB, 0x319}, // Undead Horse
            new int[]{0x3EBC, 0x317}, // Beetle
            new int[]{0x3EBD, 0x31A}, // Swamp Dragon
            new int[]{0x3EBE, 0x31F}, // Armored Swamp Dragon
            new int[]{0x3F6F, 0x9},  // Daemon
            new int[]{0x3EC3, 0x02D4}, // beetle
            new int[]{0x3EC5, 0xD5},
            new int[]{0x3F3A, 0xD5},
            new int[]{0x3E90, 0x114}, // reptalon
            new int[]{0x3E91, 0x115},  // cu sidhe
            new int[]{0x3E92, 0x11C},  // MondainSteed01
            new int[]{0x3EC6, 0x1B0},
            new int[]{0x3EC7, 0x4E6},
            new int[]{0x3EC8, 0x4E7},
        };

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "bodyconv.def");
            if (!File.Exists(path))
                return;

            List<int> list1 = new List<int>(), list2 = new List<int>(), list3 = new List<int>(), list4 = new List<int>();
            int max1 = 0, max2 = 0, max3 = 0, max4 = 0;

            using (StreamReader reader = new StreamReader(path))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line[0] == '#' || line.StartsWith("\"#"))
                        continue;

                    string[] values = Regex.Split(line, @"\t|\s+", RegexOptions.IgnoreCase);

                  /*  int index = Convert.ToInt32(values[0]);

                    int[] anim = new int[4]
                    {
                        Convert.ToInt32(values[1]),
                        -1 ,-1 ,-1
                    };

                    if (values.Length >= 3)
                    {
                        anim[1] = Convert.ToInt32(values[2]);
                        if (values.Length >= 4)
                        {
                            anim[2] = Convert.ToInt32(values[3]);
                            if (values.Length >= 5)
                                anim[3] = Convert.ToInt32(values[4]);
                        }
                    }

                    int startAnimID = -1;
                    int animFile = 1;
                    ushort realAnimID = 0;
                    ANIMATION_GROUPS_TYPE group = ANIMATION_GROUPS_TYPE.UNKNOWN;

                    if (anim[0] != -1)
                    {
                        animFile = 2;
                        realAnimID = (ushort)anim[0];
                        if (realAnimID == 68)
                            realAnimID = 122;

                        if (realAnimID >= 200)
                        {
                            startAnimID = ((realAnimID - 200) * 65) + 22000;
                            group = ANIMATION_GROUPS_TYPE.ANIMAL;
                        }
                        else
                        {
                            startAnimID = realAnimID * 110;
                            group = ANIMATION_GROUPS_TYPE.MONSTER;
                        }
                    }
                    else if (anim[1] != -1)
                    {
                        animFile = 3;
                        realAnimID = (ushort)anim[1];

                        if (realAnimID >= 200)
                        {
                            if (realAnimID >= 400)
                            {
                                startAnimID = ((realAnimID - 400) * 175) + 35000;
                                group = ANIMATION_GROUPS_TYPE.HUMAN;
                            }
                            else
                            {
                                startAnimID = (realAnimID * 65) + 9000;
                                group = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                        }
                    }
                    else if (anim[2] != -1)
                    {
                        animFile = 4;
                        realAnimID = (ushort)anim[2];

                        if (realAnimID >= 200)
                        {
                            if (realAnimID >= 400)
                            {
                                startAnimID = ((realAnimID - 400) * 175) + 35000;
                                group = ANIMATION_GROUPS_TYPE.HUMAN;
                            }
                            else
                            {
                                startAnimID = ((realAnimID - 200) * 65) + 22000;
                                group = ANIMATION_GROUPS_TYPE.ANIMAL;
                            }
                        }
                        else
                        {
                            startAnimID = realAnimID * 110;
                            group = ANIMATION_GROUPS_TYPE.MONSTER;
                        }
                    }
                    else if (anim[3] != -1)
                    {
                        animFile = 5;
                        realAnimID = (ushort)anim[3];

                        if (realAnimID == 34)
                            startAnimID = ((realAnimID - 200) * 65) + 22000;
                        else if (realAnimID >= 200)
                        {
                            if (realAnimID >= 400)
                            {
                                startAnimID = ((realAnimID - 400) * 175) + 35000;
                                group = ANIMATION_GROUPS_TYPE.HUMAN;
                            }
                            else
                            {
                                startAnimID = ((realAnimID - 200) * 65) + 22000;
                                group = ANIMATION_GROUPS_TYPE.ANIMAL;
                            }
                        }
                        else
                        {
                            startAnimID = ((realAnimID - 200) * 65) + 22000;
                            group = ANIMATION_GROUPS_TYPE.ANIMAL;
                        }
                    }

                    if (animFile != 1 && startAnimID != -1)
                    {
                        startAnimID = startAnimID * 4 * 3;
                    }
                    */

                    int original = System.Convert.ToInt32(values[0]);
                    int anim2 = System.Convert.ToInt32(values[1]);
                    int anim3 = -1, anim4 = -1, anim5 = -1;

                    if (values.Length >= 3)
                    {
                        anim3 = System.Convert.ToInt32(values[2]);

                        if (values.Length >= 4)
                        {
                            anim4 = System.Convert.ToInt32(values[3]);

                            if (values.Length >= 5)
                            {
                                anim5 = System.Convert.ToInt32(values[4]);
                            }
                        }
                    }


                    if (anim2 != -1)
                    {
                        if (anim2 == 68)
                            anim2 = 122;

                        if (original > max1)
                            max1 = original;

                        list1.Add(original);
                        list1.Add(anim2);
                    }

                    if (anim3 != -1)
                    {
                        if (original > max2)
                            max2 = original;
                        list2.Add(original);
                        list2.Add(anim3);
                    }

                    if (anim4 != -1)
                    {
                        if (original > max3)
                            max3 = original;
                        list3.Add(original);
                        list3.Add(anim4);
                    }

                    if (anim5 != -1)
                    {
                        if (original > max4)
                            max4 = original;
                        list4.Add(original);
                        list4.Add(anim5);
                    }
                    
                }
            }

            Table[0] = new int[max1 + 1];

            for (int i = 0; i < Table[0].Length; ++i)
                Table[0][i] = -1;

            for (int i = 0; i < list1.Count; i += 2)
                Table[0][list1[i]] = list1[i + 1];

            Table[1] = new int[max2 + 1];

            for (int i = 0; i < Table[1].Length; ++i)
                Table[1][i] = -1;

            for (int i = 0; i < list2.Count; i += 2)
                Table[1][list2[i]] = list2[i + 1];

            Table[2] = new int[max3 + 1];

            for (int i = 0; i < Table[2].Length; ++i)
                Table[2][i] = -1;

            for (int i = 0; i < list3.Count; i += 2)
                Table[2][list3[i]] = list3[i + 1];

            Table[3] = new int[max4 + 1];

            for (int i = 0; i < Table[3].Length; ++i)
                Table[3][i] = -1;

            for (int i = 0; i < list4.Count; i += 2)
                Table[3][list4[i]] = list4[i + 1];
        }

        public static bool HasBody(int body)
        {
            if (body >= 0)
            {
                for (int i = 0; i < Table.Length; i++)
                {
                    if (body < Table[i].Length && Table[i][body] != -1)
                        return true;
                }
            }
            return false;
        }

        public static int Convert(ref int body)
        {
            if (body >= 0)
            {
                for (int i = 0; i < Table.Length; i++)
                {
                    if (body < Table[i].Length && Table[i][body] != -1)
                    {
                        body = Table[i][body];
                        return i + 2;
                    }
                }
            }
            return 1;
        }

        public static int GetBody(int type, int index)
        {
            if (type > 5 || type == 1)
                return index;

            if (index >= 0)
            {
                var t = Table[type - 2];
                for (int i = 0; i  < t.Length; i++)
                {
                    if (t[i] == index)
                        return i;
                }
            }

            return -1;
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

        public AnimationFrame[] Frames;
    }

    public struct AnimationFrame
    {
        public short CenterX, CenterY;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AnimIdxBlock
    {
        public uint Position;
        public uint Size;
        public uint Unknown;
    }

    public struct EquipConvData
    {
        public EquipConvData(ushort graphic, ushort gump, ushort color)
        {
            Graphic = graphic; Gump = gump; Color = color;
        }

        public ushort Graphic;
        public ushort Gump;
        public ushort Color;
    }

    public struct UOPAnimationData
    {
        public uint Offset;
        public uint CompressedLength;
        public uint DecompressedLength;
        public int FileIndex;
    }
}
