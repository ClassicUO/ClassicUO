using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO.Resources
{
    internal readonly struct BodyConvInfo
    {
        public BodyConvInfo(ushort oldGraphic, ushort newGraphic, byte fileIndex, bool isUOP, ANIMATION_GROUPS_TYPE type, int offset)
        {
            OldGraphic = oldGraphic;
            NewGraphic = newGraphic;
            FileIndex = fileIndex;
            IsUOP = isUOP;
            Type = type;
            Offset = offset;
        }

        public readonly ushort OldGraphic;
        public readonly ushort NewGraphic;
        public readonly byte FileIndex;
        public readonly bool IsUOP;
        public readonly ANIMATION_GROUPS_TYPE Type;
        public readonly int Offset;
    }

    internal readonly struct AnimInfo
    {  
        public AnimInfo(ANIMATION_GROUPS_TYPE type, int offset)
        {
            Type = type;
            Offset = offset;
        }

        public readonly ANIMATION_GROUPS_TYPE Type;
        public readonly int Offset;
    }

    internal readonly struct MobTypeInfo
    {
        public MobTypeInfo(ANIMATION_GROUPS_TYPE type, long flags)
        {
            Type = type;
            Flags = flags;
        }

        public readonly ANIMATION_GROUPS_TYPE Type;
        public readonly long Flags;
    }

    internal readonly struct CorpseInfo
    {
        public CorpseInfo(ushort newGraphic, ushort color)
        {
            NewGraphic = newGraphic;
            Color = color;
        }

        public readonly ushort NewGraphic;
        public readonly ushort Color;
    }

    class AnimationsLoader2 : ResourceLoader<AnimationFrameTexture>
    {
        private readonly Dictionary<int, List<MobTypeInfo>> _mobType = new Dictionary<int, List<MobTypeInfo>>();
        private readonly Dictionary<int, List<int>> _bodies = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, List<BodyConvInfo>> _bodiesConv = new Dictionary<int, List<BodyConvInfo>>();
        private readonly Dictionary<int, List<CorpseInfo>> _corpses = new Dictionary<int, List<CorpseInfo>>();

        private readonly AnimationFrameTexture[,,][] _animations = new AnimationFrameTexture[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT, 100, 5][];
        private readonly AnimationFrameTexture[,,][] _animationsUOP = new AnimationFrameTexture[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT, 100, 5][];

        private readonly UopFileData[,] _animationIsUop = new UopFileData[Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT, 100];

        private readonly UOFileMul[] _mulFiles = new UOFileMul[5];
        private readonly UOFileUopNoFormat[] _uopFiles = new UOFileUopNoFormat[4];

        public override void Load()
        {
            int[] un = { 0x40000, 0x10000, 0x20000, 0x20000, 0x20000 };
            Dictionary<ulong, UopFileData> hashes = new Dictionary<ulong, UopFileData>();

            for (int i = 0; i < 5; i++)
            {
                string pathmul = Path.Combine(FileManager.UoFolderPath, "anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".mul");
                string pathidx = Path.Combine(FileManager.UoFolderPath, "anim" + (i == 0 ? string.Empty : (i + 1).ToString()) + ".idx");

                if (File.Exists(pathmul) && File.Exists(pathidx))
                    _mulFiles[i] = new UOFileMul(pathmul, pathidx, un[i], i == 0 ? 6 : -1);

                if (i > 0 && FileManager.ClientVersion >= ClientVersions.CV_7000)
                {
                    string pathuop = Path.Combine(FileManager.UoFolderPath, $"AnimationFrame{i}.uop");

                    if (File.Exists(pathuop))
                    {
                        _uopFiles[i - 1] = new UOFileUopNoFormat(pathuop, i - 1);
                        _uopFiles[i - 1].LoadEx(ref hashes);
                    }
                }
            }

            for (int animID = 0; animID < Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT; animID++)
            {
                for (int grpID = 0; grpID < 100; grpID++)
                {
                    string hashstring = $"build/animationlegacyframe/{animID:D6}/{grpID:D2}.bin";
                    ulong hash = UOFileUop.CreateHash(hashstring);

                    if (hashes.TryGetValue(hash, out UopFileData data) && data.Offset != 0)
                    {
                        _animationIsUop[animID, grpID] = data;

                        //for (int d = 0; d < 5; d++)
                        //{
                        //    ref var anim = ref _animations[animID, grpID, d];
                        //}

                    }
                }
            }


            ReadMobTypes();
            ReadBody();
            ReadBodyconv();
            ReadCorpse();

            //LoadAnimations(0x029A, 1, 0);
        }


        private void ReadMobTypes()
        {
            if (FileManager.ClientVersion < ClientVersions.CV_500A)
            {
                Log.Message(LogTypes.Warning, "Client version not able to load mobtype.txt");
                return;
            }

            FileInfo file = new FileInfo(Path.Combine(FileManager.UoFolderPath, "mobtypes.txt"));

            if (!file.Exists)
                return;

            using (StreamReader reader = new StreamReader(File.OpenRead(file.FullName)))
            {
                string line;

                string[] typeNames = new string[5]
                {
                    "monster", "sea_monster", "animal", "human", "equipment"
                };

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
                            if (!_mobType.TryGetValue(id, out var list) || list == null)
                            {
                                list = new List<MobTypeInfo>();
                                _mobType.Add(id, list);
                            }

                            list.Add(new MobTypeInfo((ANIMATION_GROUPS_TYPE)i, number));
                            break;
                        }
                    }
                }
            }
        }

        private void ReadBody()
        {
            FileInfo file = new FileInfo(Path.Combine(FileManager.UoFolderPath, "Body.def"));

            if (!file.Exists)
                return;

            using (DefReader defReader = new DefReader(file.FullName, 1))
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

                        if (!_bodies.TryGetValue(index, out var list) || list == null)
                        {
                            list = new List<int>();
                            _bodies.Add(index, list);
                        }

                        list.Add(checkIndex);

                    }
                }
            }
        }

        private void ReadBodyconv()
        {
            FileInfo file = new FileInfo(Path.Combine(FileManager.UoFolderPath, "Bodyconv.def"));

            if (!file.Exists)
                return;

            using (DefReader defReader = new DefReader(file.FullName))
            {
                while (defReader.Next())
                {
                    int index = defReader.ReadInt();
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


                    if (anim[0] != -1)
                    {
                        animFile = 1;
                        realAnimID = (ushort)anim[0];

                        if (index == 0x00C0 || index == 793)
                            mountedHeightOffset = -9;

                        if (realAnimID == 68)
                            realAnimID = 122;

                        if (realAnimID < 200)
                        {
                            startAnimID = realAnimID * 110;
                            groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                        }
                        else
                        {
                            if (realAnimID < 400)
                            {
                                startAnimID = realAnimID * 65 + 9000;
                                groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                            }
                            else
                            {
                                startAnimID = (realAnimID - 200) * 175;
                                groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                            }
                        }
                    }
                    else if (anim[1] != -1)
                    {
                        animFile = 2;
                        realAnimID = (ushort)anim[1];

                        if (realAnimID < 300)
                        {
                            if (FileManager.ClientVersion < ClientVersions.CV_70130)
                            {
                                startAnimID = realAnimID * 110; // 33000 + ((realAnimID - 300) * 110);
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                            else
                            {
                                startAnimID = realAnimID * 65 + 9000;
                                groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                            }
                        }
                        else
                        {
                            if (realAnimID < 400)
                            {
                                if (FileManager.ClientVersion < ClientVersions.CV_70130)
                                {
                                    startAnimID = realAnimID * 65 /*+ 9000*/;
                                    groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                                }
                                else
                                {
                                    startAnimID = 33000 + ((realAnimID - 300) * 110);
                                    groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                                }

                            }
                            else
                            {
                                startAnimID = 35000 + ((realAnimID - 400) * 175);
                                groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                            }
                        }
                    }
                    else if (anim[2] != -1)
                    {
                        animFile = 3;
                        realAnimID = (ushort)anim[2];

                        if (realAnimID < 200)
                        {
                            startAnimID = realAnimID * 110;
                            groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                        }
                        else
                        {
                            if (realAnimID < 400)
                            {
                                startAnimID = realAnimID * 65 + 9000;
                                groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                            }
                            else
                            {
                                startAnimID = (realAnimID - 200) * 175;
                                groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                            }
                        }
                    }
                    else if (anim[3] != -1)
                    {
                        animFile = 4;
                        realAnimID = (ushort)anim[3];
                        mountedHeightOffset = -9;

                        if (index == 0x0115 || index == 0x00C0)
                            mountedHeightOffset = 0;

                        if (realAnimID != 34)
                        {
                            if (realAnimID < 200)
                            {
                                startAnimID = realAnimID * 110;
                                groupType = ANIMATION_GROUPS_TYPE.MONSTER;
                            }
                            else
                            {
                                if (realAnimID < 400)
                                {
                                    startAnimID = realAnimID * 65 + 9000;
                                    groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                                }
                                else
                                {
                                    startAnimID = (realAnimID - 200) * 175;
                                    groupType = ANIMATION_GROUPS_TYPE.HUMAN;
                                }
                            }
                        }
                        else
                        {
                            startAnimID = 0x2BCA;
                            groupType = ANIMATION_GROUPS_TYPE.ANIMAL;
                        }

                    }


                    if (startAnimID != -1 && animFile != 0)
                    {

                        if (!_bodiesConv.TryGetValue(index, out var list) || list == null)
                        {
                            list = new List<BodyConvInfo>();
                            _bodiesConv.Add(index, list);
                        }

                        list.Add(new BodyConvInfo((ushort) index, (ushort) realAnimID, (byte) animFile, false, groupType, startAnimID));
                    }
                }
            }
        }

        private void ReadCorpse()
        {
            FileInfo file = new FileInfo(Path.Combine(FileManager.UoFolderPath, "Corpse.def"));

            if (!file.Exists)
                return;

            using (DefReader defReader = new DefReader(file.FullName, 1))
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

                        if (!_corpses.TryGetValue(index, out var list) || list == null)
                        {
                            list = new List<CorpseInfo>();
                            _corpses.Add(index, list);
                        }

                        list.Add(new CorpseInfo((ushort) checkIndex, (ushort) color));
                    }
                }
            }
        }


        public bool LoadAnimations(int id, int group, int dir)
        {
            ref readonly var uopData = ref _animationIsUop[id, group];

            if (uopData.Offset != 0)
            {
                return ReadUOPAnimation(id, group, dir, uopData);
            }

            return ReadAnimation(id, group, dir);
        }

        private bool ReadAnimation(int id, int group, int dir)
        {
            ref var anim = ref _animations[id, group, dir];

            if (anim == null) // first check
            {
                // check body

                if (_bodies.TryGetValue(id, out var list))
                {
                    // replace body
                    id = list[0];
                }


                if (_bodiesConv.TryGetValue(id, out var listb))
                {
                    BodyConvInfo info;

                    for (int i = 0; i < listb.Count; i++)
                    {
                        info = listb[i];

                        anim = ref _animations[info.NewGraphic, group, dir];

                        if (anim == null)
                        {
                            var file = _mulFiles[info.FileIndex];

                            (int length, _, _) = file.SeekByEntryIndex(info.Offset);
                            if (length == 0)
                                continue;

                            ReadFrame(out anim, file);

                            if (anim != null)
                                break;
                        }
                    }
                }
                else // anim.mul
                {
                    var file = _mulFiles[0];
                    int offset;

                    if (id < 200)
                        offset = id * 110;
                    else if (id < 400)
                        offset = 22000 + ((id - 200) * 65);
                    else
                        offset = 35000 + ((id - 400) * 175);

                    (int length, _, _) = file.SeekByEntryIndex(offset);
                    if (length != 0)
                        ReadFrame(out anim, file);
                }
            }

            return true;
        }

        private unsafe bool ReadUOPAnimation(int id, int group, int dir, in UopFileData uopData)
        {
            ref var anim = ref _animationsUOP[id, group, dir];

            if (anim == null)
            {
                var file = _uopFiles[uopData.FileIndex];

                // uncompress
                int decLen = (int)uopData.DecompressedLength;
                file.Seek(uopData.Offset);
                byte[] buffer = file.ReadArray<byte>((int)uopData.CompressedLength);
                byte[] decbuffer = new byte[decLen];
                ZLib.Decompress(buffer, 0, decbuffer, decLen);

                fixed (byte* ptr = decbuffer)
                {
                    DataReader reader = new DataReader();
                    reader.SetData(ptr, decLen);
                    reader.Skip(32);

                    int frameCount = reader.ReadInt();
                    int dataStart = reader.ReadInt();
                    reader.Seek(dataStart);

                    UOPFrameData[] pixelDataOffsets = new UOPFrameData[frameCount];

                    for (int i = 0; i < frameCount; i++)
                    {
                        long start = reader.Position;
                        reader.Skip(12);
                        int offset = reader.ReadInt();

                        pixelDataOffsets[i] = new UOPFrameData(start, (uint)offset);
                    }

                    frameCount /= 5;

                    int dirFrameStartIdx = frameCount * dir;

                    anim = new AnimationFrameTexture[frameCount];

                    for (int i = 0; i < frameCount; i++)
                    {
                        ref readonly var frameData = ref pixelDataOffsets[i + dirFrameStartIdx];

                        if (frameData.DataStart == 0)
                            continue;

                        reader.Seek((int) (frameData.DataStart + frameData.PixelDataOffset));

                        ushort* palette = (ushort*) reader.PositionAddress;
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
                            ushort* dataRef = ptrData;

                            int header;

                            const int DOUBLE_XOR = (0x200 << 22) | (0x200 << 12);

                            while ((header = reader.ReadInt()) != 0x7FFF7FFF)
                            {
                                header ^= DOUBLE_XOR;
                                int x = ((header >> 22) & 0x3FF) + imageCenterX - 0x200;
                                int y = ((header >> 12) & 0x3FF) + imageCenterY + imageHeight - 0x200;

                                ushort* cur = dataRef + y * imageWidth + x;
                                ushort* end = cur + (header & 0xFFF);
                                int filecounter = 0;
                                byte[] filedata = reader.ReadArray(header & 0xFFF);
                                while (cur < end)
                                    *cur++ = (ushort)(0x8000 | palette[filedata[filecounter++]]);
                            }
                        }

                        anim[i] = new AnimationFrameTexture(imageWidth, imageHeight)
                        {
                            CenterX = imageCenterX,
                            CenterY = imageCenterY
                        };

                        anim[i].SetDataHitMap16(data);
                    }


                    reader.ReleaseData();
                }
            }

            return true;
        }

        private readonly struct UOPFrameData
        {
            public UOPFrameData(long ptr, uint offset)
            {
                DataStart = ptr;
                PixelDataOffset = offset;
            }

            public readonly long DataStart;
            public readonly uint PixelDataOffset;
        }

        private unsafe void ReadFrame(out AnimationFrameTexture[] anim, UOFile file)
        {
            ushort* palette = (ushort*)file.PositionAddress;
            file.Skip(512);
            long readStart = file.Position;
            int frameCount = file.ReadInt();
            uint* frameOffset = (uint*)file.PositionAddress;

            anim = new AnimationFrameTexture[frameCount];

            for (int k = 0; k < frameCount; k++)
            {
                file.Seek(readStart + frameOffset[k]);

                short imageCenterX = file.ReadShort();
                short imageCenterY = file.ReadShort();
                short imageWidth = file.ReadShort();
                short imageHeight = file.ReadShort();

                if (imageWidth == 0 || imageHeight == 0)
                    continue;

                ushort[] data = new ushort[imageWidth * imageHeight];

                fixed (ushort* ptrData = data)
                {
                    ushort* dataRef = ptrData;

                    int header;

                    const int DOUBLE_XOR = (0x200 << 22) | (0x200 << 12);

                    while ((header = file.ReadInt()) != 0x7FFF7FFF)
                    {
                        header ^= DOUBLE_XOR;
                        int x = ((header >> 22) & 0x3FF) + imageCenterX - 0x200;
                        int y = ((header >> 12) & 0x3FF) + imageCenterY + imageHeight - 0x200;

                        ushort* cur = dataRef + y * imageWidth + x;
                        ushort* end = cur + (header & 0xFFF);
                        int filecounter = 0;
                        byte[] filedata = file.ReadArray(header & 0xFFF);
                        while (cur < end)
                            *cur++ = (ushort)(0x8000 | palette[filedata[filecounter++]]);
                    }
                }


                anim[k] = new AnimationFrameTexture(imageWidth, imageHeight)
                {
                    CenterX = imageCenterX, CenterY = imageCenterY
                };

                anim[k].SetDataHitMap16(data);
            }

        }




        public override AnimationFrameTexture GetTexture(uint id)
        {
            return null;
        }

        public override void CleanResources()
        {

        }
    }
}
