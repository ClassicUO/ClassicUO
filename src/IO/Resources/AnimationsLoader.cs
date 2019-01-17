using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.IO.Resources
{
    class AnimationsLoader : ResourceLoader<AnimationFrameTexture>
    {
        private readonly UOFileMul[] _files = new UOFileMul[5];
        private readonly UOFileUopNoFormat[] _filesUop = new UOFileUopNoFormat[4];
        private readonly List<Tuple<ushort, byte>>[] _groupReplaces = new List<Tuple<ushort, byte>>[2]
        {
            new List<Tuple<ushort, byte>>(), new List<Tuple<ushort, byte>>()
        };
        private readonly Dictionary<ushort, Dictionary<ushort, EquipConvData>> _equipConv = new Dictionary<ushort, Dictionary<ushort, EquipConvData>>();
        private byte _animGroupCount = (int)PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT;
        private readonly DataReader _reader = new DataReader();
        private readonly List<ToRemoveInfo> _usedTextures = new List<ToRemoveInfo>();
        private readonly Dictionary<Graphic, Rectangle> _animDimensionCache = new Dictionary<Graphic, Rectangle>(); 

       
        public AnimationsLoader(string path) : base(path)
        {
        }

        public AnimationsLoader(string[] paths) : base(paths)
        {
        }

        public AnimationsLoader() { }

        public ushort Color { get; set; }
        public byte AnimGroup { get; set; }
        public byte Direction { get; set; }
        public ushort AnimID { get; set; }
        //public int SittingValue { get; set; }
        public IndexAnimation[] DataIndex { get; } = new IndexAnimation[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT];
        public IReadOnlyDictionary<ushort, Dictionary<ushort, EquipConvData>> EquipConversions => _equipConv;
        public IReadOnlyList<Tuple<ushort, byte>>[] GroupReplaces => _groupReplaces;



        public override void Load()
        {
            Dictionary<ulong, UopFileData> hashes = new Dictionary<ulong, UopFileData>();

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
                        _filesUop[i - 1] = new UOFileUopNoFormat(pathuop, i - 1);
                        _filesUop[i - 1].LoadEx(ref hashes);
                    }
                }
            }

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

            for (int i = 0; i < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; i++)
            {
                ANIMATION_GROUPS_TYPE groupTye = ANIMATION_GROUPS_TYPE.UNKNOWN;
                int findID = 0;

                if (i == 735)
                {

                }

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

                DataIndex[i].Graphic = (ushort)i;
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
                            AnimIdxBlock* aidx = (AnimIdxBlock*)(address + (offset + d) * animIdxBlockSize);

                            if ((long)aidx >= maxAddress0)
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
                                ref var index = ref DataIndex[id];
                                index.Type = (ANIMATION_GROUPS_TYPE)i;
                                index.Flags = 0x80000000 | number;

                                break;
                            }
                        }
                    }
                }
            }


            using (DefReader defReader = new DefReader(Path.Combine(FileManager.UoFolderPath, "Anim1.def")))
            {
                while (defReader.Next())
                {
                    ushort group = (ushort) defReader.ReadInt();
                    int replace = defReader.ReadGroupInt();
                    _groupReplaces[0].Add(new Tuple<ushort, byte>(group, (byte) replace));
                }
            }

            using (DefReader defReader = new DefReader(Path.Combine(FileManager.UoFolderPath, "Anim2.def")))
            {
                while (defReader.Next())
                {
                    ushort group = (ushort) defReader.ReadInt();
                    int replace = defReader.ReadGroupInt();
                    _groupReplaces[1].Add(new Tuple<ushort, byte>(group, (byte)replace));
                }
            }


            if (FileManager.ClientVersion < ClientVersions.CV_305D)
                return;

            using (DefReader defReader = new DefReader(Path.Combine(FileManager.UoFolderPath, "Equipconv.def"), 5))
            {
                while (defReader.Next())
                {
                    ushort body = (ushort) defReader.ReadInt();

                    if (body >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        continue;

                    ushort graphic = (ushort) defReader.ReadInt();
                    if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        continue;

                    ushort newGraphic = (ushort) defReader.ReadInt();
                    if (newGraphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        continue;

                    int gump = defReader.ReadInt();
                    if (gump > ushort.MaxValue)
                        continue;

                    if (gump == 0)
                        gump = graphic;
                    else if (gump == 0xFFFF)
                        gump = newGraphic;

                    ushort color = (ushort) defReader.ReadInt();

                    if (!_equipConv.TryGetValue(body, out Dictionary<ushort, EquipConvData> dict))
                    {
                        _equipConv.Add(body, new Dictionary<ushort, EquipConvData>());

                        if (!_equipConv.TryGetValue(body, out dict))
                            continue;
                    }

                    dict.Add(graphic, new EquipConvData(newGraphic, (ushort)gump, color));
                }
            }

            using (DefReader defReader = new DefReader(Path.Combine(FileManager.UoFolderPath, "Bodyconv.def")))
            {
                while (defReader.Next())
                {
                    ushort index = (ushort) defReader.ReadInt();
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

                            if (defReader.PartsCount >= 5)
                            {
                                anim[3] = defReader.ReadInt();
                            }
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
                        realAnimID = (ushort)anim[0];

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
                        realAnimID = (ushort)anim[1];

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
                        realAnimID = (ushort)anim[2];

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
                        realAnimID = (ushort)anim[3];
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

                        if ((uint)startAnimID < currentIdxFile.Length)
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

                                for (byte d = 0; d < 5; d++)
                                {
                                    unsafe
                                    {
                                        AnimIdxBlock* aidx = (AnimIdxBlock*)(address + (offset + d) * animIdxBlockSize);

                                        if ((long)aidx >= (long)maxaddress)
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

            using (DefReader defReader = new DefReader(Path.Combine(FileManager.UoFolderPath, "Body.def"), 1))
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
                        int count = 0;

                        int[] ignoreGroups =
                        {
                            -1, -1
                        };

                        switch (DataIndex[checkIndex].Type)
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
                        DataIndex[index].Graphic = (ushort) checkIndex;
                        DataIndex[index].Color = (ushort) color;

                    }
                }
            }
  
            using (DefReader defReader = new DefReader(Path.Combine(FileManager.UoFolderPath, "Corpse.def"), 1))
            {
                while (defReader.Next())
                {
                    ushort index = (ushort) defReader.ReadInt();
                    if (index >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                        continue;

                    int[] group = defReader.ReadGroup();

                    ushort color = (ushort) defReader.ReadInt();

                    for (int i = 0; i < group.Length; i++)
                    {
                        int checkIndex = group[i];
                        if (checkIndex >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                            continue;

                        int[] ignoreGroups =
                        {
                            -1, -1
                        };

                        switch (DataIndex[checkIndex].Type)
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
                        DataIndex[index].Graphic = (ushort) checkIndex;
                        DataIndex[index].Color = color;
                    }
                }
            }


            byte maxGroup = 0;

            for (int animID = 0; animID < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; animID++)
            {
                for (byte grpID = 0; grpID < 100; grpID++)
                {
                    string hashstring = $"build/animationlegacyframe/{animID:D6}/{grpID:D2}.bin";
                    ulong hash = UOFileUop.CreateHash(hashstring);

                    if (hashes.TryGetValue(hash, out UopFileData data))
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
                UOFileUop animSeq = new UOFileUop(Path.Combine(FileManager.UoFolderPath, "AnimationSequence.uop"), ".bin", Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT);

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

                        _reader.SetData(decbuffer, decbuffer.Length);
                        uint animID = _reader.ReadUInt();
                        ref IndexAnimation index = ref DataIndex[animID];

                        _reader.Skip(48);

                        int replaces = _reader.ReadInt();

                        StringBuilder sb = new StringBuilder();
                        //sb.AppendLine($"AnimationID: 0x{animID:X4}\t Type: {replaces}");

                        //if (index.IsUOP)
                        //{
                        //    switch (replaces)
                        //    {
                        //        case 29:
                        //            index.Type = ANIMATION_GROUPS_TYPE.MONSTER;
                        //            break;
                        //        case 31: // what is this?
                        //            break;
                        //        case 32:
                        //            index.Type = ANIMATION_GROUPS_TYPE.EQUIPMENT;
                        //            break;
                        //        case 48:
                        //        case 68:
                        //            index.Type = ANIMATION_GROUPS_TYPE.HUMAN;
                        //            break;
                        //    }
                        //}

                        if (animID == 735)
                        {

                        }

                        for (int k = 0; k < replaces; k++)
                        {
                            int oldIdx = _reader.ReadInt();
                            uint frameCount = _reader.ReadUInt();
                            int newIDX = _reader.ReadInt();
	                        int unknown = _reader.ReadInt();

	                       sb.AppendLine($"\t\t OldIndex: {oldIdx}\t\t Frames: {frameCount}\t\t NewIndex: {newIDX}\t\t Unknown: {unknown}");

                            if (animID == 735)
                            {

                               
                                if (oldIdx == 4)
                                {
                                    //newIDX = 25;
                                }

                                if (newIDX == 4)
                                {

                                }
                            }

                            if (frameCount == 0)
                            {
                                index.Groups[oldIdx].UOPAnimData = index.Groups[newIDX].UOPAnimData;
                            }


                            _reader.Skip(48);

							var unknownA = _reader.ReadInt();

	                        //sb.AppendLine($"\t\t\t\t UnknownA: {unknownA}");

							if (unknownA > 0)
							{
								for (int x = 0; x < unknownA; x++)
								{
									var unknownB = _reader.ReadInt();
									var effectCount = _reader.ReadInt();

									//sb.AppendLine($"\t\t\t\t\t\t UnknownB: {unknownB}\t\t EffectCount: {effectCount}");

									for (int e = 0; e < effectCount; e++)
									{
										var effectUnknownA = _reader.ReadInt();
	
										var effectUnknownB = _reader.ReadInt();

										// loop here, but no data read

										var effectUnknownC = _reader.ReadInt();

										//sb.AppendLine($"\t\t\t\t\t\t\t\t effectUnknownA: {effectUnknownA}\t\t effectUnknownB: {effectUnknownB}\t\t effectUnknownC: {effectUnknownC}");

										for (int z = 0; z < effectUnknownC; z++)
										{
											var effectUnknownD = _reader.ReadInt();

											var effectUnknownE = _reader.ReadInt();

											//sb.AppendLine($"\t\t\t\t\t\t\t\t\t\t effectUnknownD: {effectUnknownD}\t\t effectUnknownE: {effectUnknownE}\t\t");
										}


										//				loop back to function here
										
										var unknownF = _reader.ReadInt();
									}

	
								}
							}

	                        var unknownC = _reader.ReadInt();

	                        //sb.AppendLine($"\t\t\t\t UnknownC: {unknownC}");

							if (unknownC > 0)
	                        {
		                        for (int x = 0; x < unknownC; x++)
		                        {
			                        var unknownD = _reader.ReadInt();

			                       //sb.AppendLine($"\t\t\t\t\t\t unknownD: {unknownD}");
								}
	                        }
                        }

                        var a = sb.ToString();
                        sb.Clear();
	                    var unknownZ = _reader.ReadInt();

	                    if (unknownZ > 0)
	                    {
	                        const int TO_CHECK = 24;
	                        const int ID = 735;

	                        if (animID == ID)
	                            sb.AppendLine("count unknownZ: " + unknownZ);

	                        

		                    for (int z = 0; z < unknownZ; z++)
		                    {
			                    var unknownZ_0 = _reader.ReadByte();
			                    var unknownZ_1 = _reader.ReadByte();
			                    var unknownZ_2 = _reader.ReadInt();

		                        int indent = 1;
                                string tab = new string('\t', indent);

		                        if (animID == ID)
		                        {
		                            sb.AppendLine(tab + "unknownZ_0: " + unknownZ_0);
		                            sb.AppendLine(tab + "unknownZ_1: " + unknownZ_1);
		                            sb.AppendLine(tab + "unknownZ_2: " + unknownZ_2);
		                            sb.AppendLine();
                                }

                                if (unknownZ_0 == TO_CHECK && animID == ID)
		                        {

                                }

		                        if (unknownZ_1 == TO_CHECK && animID == ID)
		                        {

		                        }

		                        if (unknownZ_2 == TO_CHECK && animID == ID)
		                        {

		                        }

                                // I don't know yet
                                {
				                    var unknown_float_A = 0.0;

				                    if (unknownZ_1 < 0)
					                    unknown_float_A = 0.0;
                                }

		                        /////////////////////////////////////////////////////////
                                var unknownZ_3 = _reader.ReadInt();

		                        if (unknownZ_3 >= 0)
		                        {
		                            sb.AppendLine(tab + "count unknownZ_3: " + unknownZ_3);
                                    tab = new string('\t', ++indent);
		                        }
                                
		                        for (int y = 0; y < unknownZ_3; y++)
			                    {
				                    var unknownY_0 = _reader.ReadByte();
				                    var unknownY_1 = _reader.ReadInt();

			                        if (animID == ID)
			                        {
			                            sb.AppendLine(tab + "unknownY_0: " + unknownY_0);
			                            sb.AppendLine(tab + "unknownY_1: " + unknownY_1);
			                            sb.AppendLine();
                                    }

                                    if (unknownY_0 == TO_CHECK && animID == ID)
			                        {

			                        }

			                        if (unknownY_1 == TO_CHECK && animID == ID)
			                        {

			                        }

			                        //index.Groups[unknownY_0].UOPAnimData = index.Groups[unknownY_1].UOPAnimData;

                                }

		                        if (unknownZ_3 > 0)
		                            indent--;
                                /////////////////////////////////////////////////////////



		                        /////////////////////////////////////////////////////////
                                var unknownZ_4 = _reader.ReadInt();

		                        if (unknownZ_4 >= 0)
		                        {
		                            sb.AppendLine(tab + "count unknownZ_4: " + unknownZ_4);
                                    tab = new string('\t', ++indent);
		                        }

                                for (int x = 0; x < unknownZ_4; x++)
			                    {
				                    var unknownX_0 = _reader.ReadByte();
				                    var unknownX_1 = _reader.ReadInt();
				                    var unknownX_2 = _reader.ReadInt();

			                        if (animID == ID)
			                        {
			                            sb.AppendLine(tab + "unknownX_0: " + unknownX_0);
			                            sb.AppendLine(tab + "unknownX_1: " + unknownX_1);
			                            sb.AppendLine(tab + "unknownX_2: " + unknownX_2);
			                            sb.AppendLine();
                                    }

                                    if (unknownX_0 == TO_CHECK && animID == ID)
			                        {

			                        }

			                        if (unknownX_1 == TO_CHECK && animID == ID)
			                        {

			                        }

			                        if (unknownX_2 == TO_CHECK && animID == ID)
			                        {

			                        }

			                        //index.Groups[unknownX_0].UOPAnimData = index.Groups[unknownX_1].UOPAnimData;

                                    //index.Groups[unknownX_0] = index.Groups[unknownX_1];
                                    //index.Groups[unknownX_0] = index.Groups[unknownX_2];

                                }

		                        if (unknownZ_4 > 0)
		                            indent--;
                                /////////////////////////////////////////////////////////



		                        /////////////////////////////////////////////////////////
                                var unknownZ_5 = _reader.ReadInt();

		                        if (unknownZ_5 >= 0)
		                        {
		                            sb.AppendLine(tab + "count unknownZ_5: " + unknownZ_5);
                                    tab = new string('\t', ++indent);
		                        }

                                for (int w = 0; w < unknownZ_5; w++)
			                    {
				                    var unknownW_0 = _reader.ReadByte();
				                    var unknownW_1 = _reader.ReadByte();

			                        if (animID == ID)
			                        {
			                            sb.AppendLine(tab + "unknownW_0: " + unknownW_0);
			                            sb.AppendLine(tab + "unknownW_1: " + unknownW_1);
			                            sb.AppendLine();
                                    }

                                    if (unknownW_0 == TO_CHECK && animID == ID)
			                        {

			                        }

			                        if (unknownW_1 == TO_CHECK && animID == ID)
			                        {

			                        }

			                        var unknownW_2 = _reader.ReadInt();


			                        if (unknownW_2 >= 0)
			                        {
			                            sb.AppendLine(tab + "count unknownW_2: " + unknownW_2);
                                        tab = new string('\t', ++indent);
			                        }

                                    for (int v = 0; v < unknownW_2; v++)
				                    {
					                    var unknownV_0 = _reader.ReadInt();

				                        if (animID == ID)
				                        {
				                            sb.AppendLine(tab + "unknownV_0: " + unknownV_0);
				                            sb.AppendLine();
				                        }

                                        if (unknownV_0 == TO_CHECK && animID == ID)
				                        {

				                        }

                                    }

			                        if (unknownW_2 > 0)
			                        {
			                            indent--;
			                        }
                                }

		                        if (unknownZ_5 > 0)
		                            indent--;
		                        /////////////////////////////////////////////////////////

                            }
                        }

                        int toread = (int)(_reader.Length - _reader.Position);

                        if (animID == 735)
                            a = sb.ToString();

	                    if (toread > 0)
		                    throw new Exception("More data");

                       // file.WriteAsync(sb.ToString());
                    }
                }

                //file.Dispose();
                animSeq.Dispose();
            }
        }
   
        public override AnimationFrameTexture GetTexture(uint id)
        {
            return ResourceDictionary[id];
        }

        public override void CleanResources()
        {
            throw new NotImplementedException();
        }

        public void UpdateAnimationTable(uint flags)
        {
            for (int i = 0; i < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; i++)
            {
                for (int g = 0; g < 100; g++)
                {
                    for (int d = 0; d < 5; d++)
                    {
                        bool replace = DataIndex[i].Groups[g].Direction[d].FileIndex >= 3;

                        if (DataIndex[i].Groups[g].Direction[d].FileIndex == 1)
                            replace = World.ClientLockedFeatures.LBR;
                        else if (DataIndex[i].Groups[g].Direction[d].FileIndex == 2)
                            replace = World.ClientLockedFeatures.AOS;

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

        /* public readonly struct SittingInfoData
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
            new SittingInfoData(0x0B5D, 6, 6, 6, 6, 0 ,8, true),
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
            new SittingInfoData(0x0CF3, -1, 2, -1 , 6, 2, 8, false),
            new SittingInfoData(0x0CF4, -1, 2, -1 , 6, 2, 8, false),
            new SittingInfoData(0x0CF6, 0, -1, 4, -1, 2, 8, false),
            new SittingInfoData(0x0CF7, 0, -1, 4, -1, 2, 8, false),
            new SittingInfoData(0x11FC, 0, 2, 4, 6, 2, 7, false),
            new SittingInfoData(0x1218, 4, 4, 4, 4, 4, 4, false),
            new SittingInfoData(0x1219, 2, 2, 2, 2, 4, 4, false),
            new SittingInfoData(0x121A, 0, 0, 0, 0, 0, 8, true),
            new SittingInfoData(0x121B, 6, 6, 6, 6, 0, 8, true),
            new SittingInfoData(0x1527, 2, 2, 2, 2, 0, 0, false),
            new SittingInfoData(0x1771, 0, 2, 4, 6, 0 , 0, false),
            new SittingInfoData(0x1776, 0, 2, 4, 6, 0 , 0, false),
            new SittingInfoData(0x1779, 0, 2, 4, 6, 0 , 0, false),       
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


        public void FixSittingDirection(ref byte layerDirection, ref bool mirror, ref int x, ref int y)
        {
            ref var data = ref SittingInfos[SittingValue - 1];

            switch (Direction)
            {
                case 7:
                case 0:
                {
                    if (data.Direction1 == -1)
                    {
                        if (Direction == 7)
                            Direction = (byte) data.Direction4;
                        else
                            Direction = (byte) data.Direction2;
                    }
                    else
                        Direction = (byte) data.Direction1;

                    break;
                }
                case 1:
                case 2:
                {
                    if (data.Direction2 == -1)
                    {
                        if (Direction == 1)
                            Direction = (byte) data.Direction1;
                        else
                            Direction = (byte) data.Direction3;
                    }
                    else
                        Direction = (byte) data.Direction2;

                    break;
                }
                case 3:
                case 4:
                {
                    if (data.Direction3 == -1)
                    {
                        if (Direction == 3)
                            Direction = (byte) data.Direction2;
                        else
                            Direction = (byte) data.Direction4;
                    }
                    else
                        Direction = (byte) data.Direction3;

                    break;
                }
                case 5:
                case 6:
                {
                    if (data.Direction4 == -1)
                    {
                        if (Direction == 5)
                            Direction = (byte) data.Direction3;
                        else
                            Direction = (byte) data.Direction1;
                    }
                    else
                        Direction = (byte) data.Direction4;

                    break;
                }
                default:
                    break;
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
                    y += 23 + data.MirrorOffsetY;
                    x += offsX - 4;
                }
                else
                {
                    y += data.OffsetY + 9;
                }
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
                    y += 9 + data.OffsetY;
                    x -= offsX + 1;
                }
            }
        }
        
       */

        public ANIMATION_GROUPS GetGroupIndex(ushort graphic)
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

        public byte GetDieGroupIndex(ushort id, bool second)
        {
            switch (DataIndex[id].Type)
            {
                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    return (byte)(second ? LOW_ANIMATION_GROUP.LAG_DIE_2 : LOW_ANIMATION_GROUP.LAG_DIE_1);
                case ANIMATION_GROUPS_TYPE.MONSTER:
                case ANIMATION_GROUPS_TYPE.SEA_MONSTER:

                    return (byte)(second ? HIGHT_ANIMATION_GROUP.HAG_DIE_2 : HIGHT_ANIMATION_GROUP.HAG_DIE_1);
                case ANIMATION_GROUPS_TYPE.HUMAN:
                case ANIMATION_GROUPS_TYPE.EQUIPMENT:

                    return (byte)(second ? PEOPLE_ANIMATION_GROUP.PAG_DIE_2 : PEOPLE_ANIMATION_GROUP.PAG_DIE_1);
            }

            return 0;
        }

        public  bool AnimationExists(ushort graphic, byte group)
        {
            bool result = false;

            if (graphic < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT && group < 100)
            {
                ref AnimationDirection d = ref DataIndex[graphic].Groups[group].Direction[0];
                result = d.Address != 0 || d.IsUOP;
            }

            return result;
        }

        public  bool LoadDirectionGroup(ref AnimationDirection animDir)
        {
            if (animDir.IsUOP)
                return TryReadUOPAnimDimension(ref animDir);

            if (animDir.Address == 0)
                return false;
            UOFileMul file = _files[animDir.FileIndex];

            // long to int can loss data
            _reader.SetData(file.StartAddress + (int)animDir.Address, animDir.Size);
            ReadFramesPixelData(ref animDir);

            _reader.ReleaseData();
            return true;
        }

        private  unsafe bool TryReadUOPAnimDimension(ref AnimationDirection animDirection)
        {
            ref AnimationGroup dataindex = ref DataIndex[AnimID].Groups[AnimGroup];
            UopFileData animData = dataindex.UOPAnimData;

            if (animData.FileIndex == 0 && animData.CompressedLength == 0 && animData.DecompressedLength == 0 && animData.Offset == 0)
            {
                Log.Message(LogTypes.Warning, "uop animData is null");

                return false;
            }

            animDirection.LastAccessTime = Engine.Ticks;
            int decLen = (int)animData.DecompressedLength;
            UOFileUopNoFormat file = _filesUop[animData.FileIndex];
            file.Seek(animData.Offset);
            byte[] buffer = file.ReadArray<byte>((int)animData.CompressedLength);
            byte[] decbuffer = new byte[decLen];
            ZLib.Decompress(buffer, 0, decbuffer, decLen);

            _reader.SetData(decbuffer, decLen);
            _reader.Skip(8);
            int dcsize = _reader.ReadInt();
            int animID = _reader.ReadInt();
            _reader.Skip(16);
            int frameCount = _reader.ReadInt();
            IntPtr dataStart = _reader.StartAddress + _reader.ReadInt();
            _reader.SetData(dataStart);
            //List<UOPFrameData> pixelDataOffsets = new List<UOPFrameData>();

            UOPFrameData[] pixelDataOffsets = new UOPFrameData[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                IntPtr dataStart1 = _reader.PositionAddress;
                ushort group = _reader.ReadUShort();
                short frameID = _reader.ReadShort();
                _reader.Skip(8);
                uint pixelOffset = _reader.ReadUInt();
                //int vsize = pixelDataOffsets.Count;

                UOPFrameData data = new UOPFrameData(dataStart1, frameID, pixelOffset);
                pixelDataOffsets[i] = data;
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

            if (animDirection.FramesHashes != null && animDirection.FramesHashes.Length > 0)
            {
                Log.Message(LogTypes.Panic, "MEMORY LEAK UOP ANIM");
            }

            animDirection.FramesHashes = new uint[animDirection.FrameCount];

            for (int i = 0; i < animDirection.FrameCount; i++)
            {
                if (animDirection.FramesHashes[i] != 0)
                    continue;

                UOPFrameData frameData = pixelDataOffsets[i + dirFrameStartIdx];

                if (frameData.DataStart == IntPtr.Zero)
                    continue;
                _reader.SetData(frameData.DataStart + (int)frameData.PixelDataOffset);
                ushort* palette = (ushort*)_reader.StartAddress;
                _reader.Skip(512);
                short imageCenterX = _reader.ReadShort();
                short imageCenterY = _reader.ReadShort();
                short imageWidth = _reader.ReadShort();
                short imageHeight = _reader.ReadShort();

                if (imageWidth == 0 || imageHeight == 0)
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

                uint uniqueAnimationIndex = (uint)(((AnimID & 0xfff) << 20) + ((AnimGroup & 0x3f) << 12) + ((Direction & 0x0f) << 8) + (i & 0xFF));

                AnimationFrameTexture f = new AnimationFrameTexture(imageWidth, imageHeight)
                {
                    CenterX = imageCenterX,
                    CenterY = imageCenterY
                };

                f.SetDataHitMap16(pixels);
                animDirection.FramesHashes[i] = uniqueAnimationIndex;
                ResourceDictionary.Add(uniqueAnimationIndex, f);
            }

            _usedTextures.Add(new ToRemoveInfo(AnimID, AnimGroup, Direction));

            _reader.ReleaseData();

            return true;
        }

        private unsafe void ReadFramesPixelData(ref AnimationDirection animDir)
        {
            animDir.LastAccessTime = Engine.Ticks;
            ushort* palette = (ushort*)_reader.StartAddress;
            _reader.Skip(512);
            IntPtr dataStart = _reader.PositionAddress;
            uint frameCount = _reader.ReadUInt();
            animDir.FrameCount = (byte)frameCount;
            uint* frameOffset = (uint*)_reader.PositionAddress;

            if (animDir.FramesHashes != null && animDir.FramesHashes.Length > 0)
            {
                Log.Message(LogTypes.Panic, "MEMORY LEAK MUL ANIM");
            }

            animDir.FramesHashes = new uint[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                if (animDir.FramesHashes[i] != 0)
                    continue;
                _reader.SetData(dataStart + (int)frameOffset[i]);
                short imageCenterX = _reader.ReadShort();
                short imageCenterY = _reader.ReadShort();
                short imageWidth = _reader.ReadShort();
                short imageHeight = _reader.ReadShort();

                if (imageWidth == 0 || imageHeight == 0) continue;
                int wantSize = imageWidth * imageHeight;
                ushort[] pixels = new ushort[wantSize];
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
                    int block = y * imageWidth + x;

                    for (int k = 0; k < runLength; k++)
                    {
                        ushort val = palette[_reader.ReadByte()];

                        if (val > 0)
                            pixels[block] = (ushort)(0x8000 | val);
                        else
                            pixels[block] = 0;
                        block++;
                    }

                    header = _reader.ReadUInt();
                }

                uint uniqueAnimationIndex = (uint)(((AnimID & 0xfff) << 20) + ((AnimGroup & 0x3f) << 12) + ((Direction & 0x0f) << 8) + (i & 0xFF));

                AnimationFrameTexture f = new AnimationFrameTexture(imageWidth, imageHeight)
                {
                    CenterX = imageCenterX,
                    CenterY = imageCenterY
                };

                f.SetDataHitMap16(pixels);

                animDir.FramesHashes[i] = uniqueAnimationIndex;
                ResourceDictionary.Add(uniqueAnimationIndex, f);
            }

            _usedTextures.Add(new ToRemoveInfo(AnimID, AnimGroup, Direction));
        }

        public void GetAnimationDimensions(byte frameIndex, Graphic id, byte dir, byte animGroup, out int x, out int y, out int w, out int h)
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

                if (dir < 5)
                {
                    AnimationDirection direction = DataIndex[id].Groups[animGroup].Direction[dir];
                    int fc = direction.FrameCount;

                    if (fc > 0)
                    {
                        if (frameIndex >= fc) frameIndex = 0;

                        if (direction.FramesHashes != null)
                        {
                            AnimationFrameTexture animationFrameTexture = GetTexture(direction.FramesHashes[frameIndex]);
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
                        _reader.SetData(file.StartAddress + (int)direction1.Address, direction1.Size);
                        ReadFrameDimensionData(frameIndex, out x, out y, out w, out h);
                        _animDimensionCache.Add(id, new Rectangle(x, y, w, h));
                        _reader.ReleaseData();
                        return;
                    }
                }
                else if (direction1.IsUOP)
                {
                    UopFileData animDataStruct = DataIndex[AnimID].Groups[AnimGroup].UOPAnimData;

                    if (!(animDataStruct.FileIndex == 0 && animDataStruct.CompressedLength == 0 && animDataStruct.DecompressedLength == 0 && animDataStruct.Offset == 0))
                    {
                        int decLen = (int)animDataStruct.DecompressedLength;
                        UOFileUopNoFormat file = _filesUop[animDataStruct.FileIndex];
                        file.Seek(animDataStruct.Offset);
                        byte[] buffer = file.ReadArray<byte>((int)animDataStruct.CompressedLength);
                        byte[] decbuffer = new byte[decLen];
                        ZLib.Decompress(buffer, 0, decbuffer, decLen);

                        _reader.SetData(decbuffer, decLen);
                        _reader.Skip(8);
                        int dcsize = _reader.ReadInt();
                        int animID = _reader.ReadInt();
                        _reader.Skip(16);
                        int frameCount = _reader.ReadInt();
                        IntPtr dataStart = _reader.StartAddress + _reader.ReadInt();
                        _reader.SetData(dataStart);

                        _reader.Skip(2);
                        short frameID = _reader.ReadShort();
                        _reader.Skip(8);
                        uint pixelOffset = _reader.ReadUInt();

                        UOPFrameData data = new UOPFrameData(dataStart, frameID, pixelOffset);

                        _reader.SetData(data.DataStart + (int)data.PixelDataOffset);
                        _reader.Skip(512);
                        x = _reader.ReadShort();
                        y = _reader.ReadShort();
                        w = _reader.ReadShort();
                        h = _reader.ReadShort();
                        _animDimensionCache.Add(id, new Rectangle(x, y, w, h));
                        _reader.ReleaseData();
                        return;
                    }
                }
            }

            x = 0;
            y = 0;
            w = 0;
            h = 0;
        }

        private unsafe void ReadFrameDimensionData(byte frameIndex, out int x, out int y, out int w, out int h)
        {
            _reader.Skip(512);
            byte* dataStart = (byte*)_reader.PositionAddress;
            uint frameCount = _reader.ReadUInt();
            if (frameCount > 0 && frameIndex >= frameCount) frameIndex = 0;

            if (frameIndex < frameCount)
            {
                uint* frameOffset = (uint*)_reader.PositionAddress;
                _reader.SetData((IntPtr)(dataStart + frameOffset[frameIndex]));
                x = _reader.ReadShort();
                y = _reader.ReadShort();
                w = _reader.ReadShort();
                h = _reader.ReadShort();
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
                ToRemoveInfo info = _usedTextures[i];
                ref AnimationDirection dir = ref DataIndex[info.AnimID].Groups[info.Group].Direction[info.Direction];

                if (dir.LastAccessTime < ticks)
                {
                    for (int j = 0; j < dir.FrameCount; j++)
                    {
                        ref uint hash = ref dir.FramesHashes[j];

                        if (ResourceDictionary.TryGetValue(hash, out var texture))
                        {
                            texture?.Dispose();
                            ResourceDictionary.Remove(hash);
                            hash = 0;
                        }
                    }

                    dir.FrameCount = 0;
                    dir.FramesHashes = null;
                    dir.LastAccessTime = 0;
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
                ToRemoveInfo info = _usedTextures[i];
                ref AnimationDirection dir = ref DataIndex[info.AnimID].Groups[info.Group].Direction[info.Direction];


                for (int j = 0; j < dir.FrameCount; j++)
                {
                    ref uint hash = ref dir.FramesHashes[j];

                    if (ResourceDictionary.TryGetValue(hash, out var texture) && texture != null)
                    {
                        texture.Dispose();
                        ResourceDictionary.Remove(hash);
                        hash = 0;
                    }
                }

                dir.FrameCount = 0;
                dir.FramesHashes = null;
                dir.LastAccessTime = 0;
                _usedTextures.RemoveAt(i--);

            }
        }


        private readonly struct ToRemoveInfo
        {
            public ToRemoveInfo(int animID, int group, int direction)
            {
                AnimID = animID;
                Group = group;
                Direction = direction;
            }

            public readonly int AnimID;
            public readonly int Group;
            public readonly int Direction;
        }

        private readonly struct UOPFrameData
        {
            public UOPFrameData(IntPtr ptr, short id, uint offset)
            {
                DataStart = ptr;
                FrameID = id;
                PixelDataOffset = offset;
            }

            public readonly IntPtr DataStart;
            public readonly short FrameID;
            public readonly uint PixelDataOffset;
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

    internal struct IndexAnimation
    {
        public ushort Graphic;
        public ushort Color;
        public ANIMATION_GROUPS_TYPE Type;
        public uint Flags;
        public sbyte MountedHeightOffset;
        public bool IsUOP;

        // 100
        public AnimationGroup[] Groups;
    }

    internal struct AnimationGroup
    {
        // 5
        public AnimationDirection[] Direction;
        public UopFileData UOPAnimData;
    }

    internal struct AnimationDirection
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
        public uint[] FramesHashes;
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

    internal readonly struct UopFileData
    {
        public UopFileData(uint offset, uint clen, uint dlen, int index)
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
