using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO.Compression;

namespace ClassicUO.Assets
{
    public enum ANIMATION_GROUPS_TYPE
    {
        MONSTER = 0,
        SEA_MONSTER,
        ANIMAL,
        HUMAN,
        EQUIPMENT,
        UNKNOWN
    }

    public static class Animations
    {
        private static List<UOFile> _files = new List<UOFile>();
        

        public static void Load()
        {
            _files.Add(new UOFileMul(Path.Combine(FileManager.UoFolderPath, "anim.mul"), Path.Combine(FileManager.UoFolderPath, "anim.idx"), 0x40000, 6));
            _files.Add(new UOFileMul(Path.Combine(FileManager.UoFolderPath, "anim2.mul"), Path.Combine(FileManager.UoFolderPath, "anim2.idx"), 0x10000));
            _files.Add(new UOFileMul(Path.Combine(FileManager.UoFolderPath, "anim3.mul"), Path.Combine(FileManager.UoFolderPath, "anim3.idx"), 0x20000));
            _files.Add(new UOFileMul(Path.Combine(FileManager.UoFolderPath, "anim4.mul"), Path.Combine(FileManager.UoFolderPath, "anim4.idx"), 0x20000));
            _files.Add(new UOFileMul(Path.Combine(FileManager.UoFolderPath, "anim5.mul"), Path.Combine(FileManager.UoFolderPath, "anim5.idx"), 0x20000));

            for (int i = 1; i < 5; i++)
            {
                string filepath = Path.Combine(FileManager.UoFolderPath, string.Format("AnimationFrame{0}.uop", i));
                if (File.Exists(filepath))
                    _files.Add(new UOFileUopAnimation(filepath));
            }
        }

        public static AnimationFrame[] GetAnimation(int body, int action, int direction, ref int hue)
        {
            BodyDef.Translate(ref body, ref hue);
            int type = GraphicHelper.Convert(ref body);
            
            GetFileToRead(body, action, direction, type, out UOFile file, out int index);

            bool flip = direction > 4;

            if (file is UOFileUopAnimation uopfile)
            {
                uopfile.Seek(uopfile.Entries[index].Offset);
                return LoadAnimationUop(uopfile, body, direction);
            }
            
            file.Seek(file.Entries[index].Offset);
            return LoadAnimation(file);
        }

        private static AnimationFrame[] LoadAnimation(UOFile file)
        {
            ushort[] palette = new ushort[0x100];
            for (int i = 0; i < palette.Length; i++)
                palette[i] = (ushort)(file.ReadUShort() ^ 0x8000);

            int start = (int)file.Position;
            int frameCount = file.ReadInt();

            int[] lookups = new int[frameCount];
            for (int i = 0; i < lookups.Length; i++)
                lookups[i] = start + file.ReadInt();

            AnimationFrame[] frames = new AnimationFrame[frameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                file.Seek(lookups[i]);
                frames[i] = new AnimationFrame(palette, file);
            }
            return frames;
        }

        private struct UopDataFrame
        {
            public short ID;
            public int Offset;
            public int Start;

            public static UopDataFrame Null = new UopDataFrame()
            {
                ID = 0,
                Offset = 0,
                Start = -1,
            };
        }

        private static unsafe AnimationFrame[] LoadAnimationUop(UOFileUopAnimation file, int body, int direction)
        {
            int start = 0;

            file.Uncompress(body);

            file.Skip(8);
            int dcsize = file.ReadInt();
            int animid = file.ReadInt();
            file.Skip(16);
            int framecount = file.ReadInt();
            int datastart = start + file.ReadInt();
            file.Seek(datastart);

            List<UopDataFrame> datas = new List<UopDataFrame>();
            for (int i = 0; i < framecount; i++)
            {
                UopDataFrame data = new UopDataFrame()
                {
                    Start = (int)file.Position,
                };
                file.Skip(2);
                data.ID = file.ReadShort();
                file.Skip(8);
                data.Offset = file.ReadInt();

                int vsize = datas.Count;
                if (vsize + 1 != data.ID)
                {
                    while (vsize + 1 != data.ID)
                    {
                        datas.Add(UopDataFrame.Null);
                        vsize++;
                    }
                }
                datas.Add(data);
            }

            int animframecount = datas.Count / 5;

            AnimationFrame[] frames = new AnimationFrame[animframecount];

            int dir = direction & 7;
            if (dir > 4)
                dir = dir - (dir - 4) * 2;

            int framestartidx = animframecount * dir;

            for (int i = 0; i < animframecount; i++)
            {
                UopDataFrame data = datas[i + framestartidx];
                if (data.Start == -1)
                {
                    frames[i] = AnimationFrame.Null;
                    continue;
                }

                file.Seek(data.Start + data.Offset);

                ushort[] palette = new ushort[0x100];
                for (int a = 0; a < palette.Length; a++)
                    palette[a] = (ushort)(file.ReadUShort() ^ 0x8000);

                frames[i] = new AnimationFrame(palette, file);
            }

            return frames;
        }

        private static void GetFileToRead(int body, int action, int direction, int type, out UOFile file, out int index)
        {
            switch (type)
            {
                default:
                case 1:
                    if (body < 200)
                        index = body * 110;
                    else if (body < 400)
                        index = 22000 + ((body - 200) * 65);
                    else
                        index = 35000 + ((body - 400) * 175);

                    if (index >= _files[0].Entries.Length || (body < _files[5].Entries.Length && _files[5].Entries[body].IsUOP ))
                    {
                        file = _files[5];
                        index = file.Entries[body].AnimID;
                    }
                    else
                        file = _files[0];                    
                    break;
                case 2:
                    if (body < 200)
                        index = body * 110;
                    else
                        index = 22000 + ((body - 200) * 65);

                    if (index >= _files[1].Entries.Length || (body < _files[6].Entries.Length && _files[6].Entries[body].IsUOP))
                    {
                        file = _files[6];
                        index = file.Entries[body].AnimID;
                    }
                    else
                        file = _files[1];

                    break;
                case 3:
                    if (body < 300)
                        index = body * 65;
                    else if (body < 400)
                        index = 33000 + ((body - 300) * 110);
                    else
                        index = 35000 + ((body - 400) * 175);

                    if (index >= _files[2].Entries.Length || (index < _files[7].Entries.Length && _files[7].Entries[body].IsUOP))
                    {
                        file = _files[7];
                        index = file.Entries[body].AnimID;
                    }
                    else
                        file = _files[2];

                    break;
                case 4:
                    if (body < 200)
                        index = body * 110;
                    else if (body < 400)
                        index = 22000 + ((body - 200) * 65);
                    else
                        index = 35000 + ((body - 400) * 175);

                    if (index >= _files[3].Entries.Length || (body < _files[8].Entries.Length && _files[8].Entries[body].IsUOP))
                    {
                        file = _files[8];
                        index = file.Entries[body].AnimID;
                    }
                    else
                        file = _files[3];

                    break;
                case 5:
                    // NB: maybe wrong .uop
                    file = _files[4];
                    if ((body < 200) && (body != 34)) // looks strange, though it works.
                        index = body * 110;
                    else if (body < 400)
                        index = 22000 + ((body - 200) * 65);
                    else
                        index = 35000 + ((body - 400) * 175);
                    break;
            }

            index += action * 5;

            if (direction <= 4)
                index += direction;
            else
                index += direction - (direction - 4) * 2;
        }
    }

    public class AnimationFrame
    {
        public static readonly AnimationFrame Null = new AnimationFrame();
        public static readonly AnimationFrame[] Empty = { Null };

        const int DOUBLE_XOR = (0x200 << 22) | (0x200 << 12);
        const int END_OF_FRAME = 0x7FFF7FFF;

        private AnimationFrame()
        {
            CenterX = 0;
            CenterY = 0;
        }

        public unsafe AnimationFrame(ushort[] palette, UOFile file)
        {
            int centerX = file.ReadShort();
            int centerY = file.ReadShort();
            int width = file.ReadUShort();
            int height = file.ReadUShort();

            if (width == 0 || height == 0)
                return;

            // sittings ?

            ushort[] data = new ushort[width * height];

            fixed (ushort* pdata = data)
            {
                ushort* dataRef = pdata;

                int header;

                while ((header = file.ReadInt()) != END_OF_FRAME)
                {
                    header ^= DOUBLE_XOR;

                    int x = ((header >> 22) & 0x3FF) + centerX - 0x200;
                    int y = ((header >> 12) & 0x3FF) + centerY + height - 0x200;

                    ushort* cur = dataRef + y * width + x;
                    ushort* end = cur + (header & 0xFFF);
                    int filecount = 0;
                    byte[] filedata = file.ReadArray<byte>(header & 0xFFF);
                    while (cur < end)
                        *cur++ = palette[filedata[filecount++]];
                }

            }

            CenterX = centerX;
            CenterY = centerY;
            Data = data;
        }

        public int CenterX { get; }
        public int CenterY { get; }
        public ushort[] Data { get; }
    }

    public class UOFileUopAnimation : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;

        public UOFileUopAnimation(string path) : base(path)
        {
            Load();
        }

        public unsafe void Uncompress(int index)
        {
            /*var e = Entries[index];
            Seek(e.Offset);*/
            (int length, int extra, bool patcher) = SeekByEntryIndex(index);
            byte[] buffer = ReadArray<byte>(length);
            int clen = length;
            int dlen = Entries[index].DecompressedLength;

            byte[] decbuffer = new byte[dlen];
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                ms.Seek(2, SeekOrigin.Begin);
                using (DeflateStream stream = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    for (int i = 0; i < dlen; i++)
                        decbuffer[i] = (byte)stream.ReadByte();
                }
            }

            fixed (byte* ptr = decbuffer)
            {
                _ptr = ptr;
                _position = 0;
                _length = decbuffer.Length;
            }
        }


        protected override void Load()
        {
            base.Load();

            Seek(0);
            if (ReadInt() != UOP_MAGIC_NUMBER)
                throw new ArgumentException("Bad uop file");

            Skip(8);
            long nextblock = ReadLong();
            Skip(4);

            Seek(nextblock);

            Dictionary<ulong, UOFileIndex3D> hashes = new Dictionary<ulong, UOFileIndex3D>();      
            Entries = new UOFileIndex3D[4096];

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

                    UOFileIndex3D data = new UOFileIndex3D(offset + headerLength, compressedLength, 0, decompressedLength);


                    hashes.Add(hash, data);
                }
                Seek(nextblock);
            } while (nextblock != 0);

            int idx = 0;
            for (int animID = 0; animID < 2048; animID++)
            {
                for (int grpID = 0; grpID < 100; grpID++)
                {
                    string hashstring = string.Format("build/animationlegacyframe/{0:D6}/{1:D2}.bin", animID, grpID);
                    ulong hash = UOFileUop.CreateHash(hashstring);

                    if (hashes.TryGetValue(hash, out var data))
                    {
                        if (data.AnimID <= 0)
                            data.AnimID = idx++;
                        Entries[animID + grpID] = data;
                    }
                }
            }
        }
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

    public class IndexAnimation
    {
        public IndexAnimation()
        {

        }


        public ushort Graphic { get; set; }
        public ushort Color { get; set; }
        public ANIMATION_GROUPS_TYPE Type { get; set; }
        public uint Flags { get; set; }
        public byte MountedHeightOffset { get; set; }
        public bool IsUOP { get; set; }
    }

    public class AnimationGroup
    {
        public AnimationGroup()
        {

        }

        
    }
}
