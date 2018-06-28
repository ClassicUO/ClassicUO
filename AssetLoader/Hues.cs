using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.AssetsLoader
{
    public static class Hues
    {
        private static UOFileMul _file;

        private static int _huesCount;
        private static FloatHues[] _palette;
        private static ushort[] _radarcol;
        private static HuesGroup[] _huesRange;

        public static HuesGroup[] HuesRange => _huesRange;
        public static int HuesCount => _huesCount;
        public static FloatHues[] Palette => _palette;
        public static ushort[] RadarCol => _radarcol;

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "hues.mul");
            if (!File.Exists(path))
                throw new FileNotFoundException();

            _file = new UOFileMul(path);


            int groupSize = Marshal.SizeOf<HuesGroup>();

            int entrycount = (int)_file.Length / groupSize;

            _huesCount = entrycount * 8;
            _huesRange = new HuesGroup[entrycount];

            ulong addr = (ulong)_file.StartAddress;

            for (int i = 0; i < entrycount; i++)
                _huesRange[i] = Marshal.PtrToStructure<HuesGroup>((IntPtr)(addr + (ulong)(i * groupSize)));

            path = Path.Combine(FileManager.UoFolderPath, "radarcol.mul");
            if (!File.Exists(path))
                throw new FileNotFoundException();

            UOFileMul radarcol = new UOFileMul(path);

            int size = (int)radarcol.Length / 2;
           // _radarcol = new ushort[size];
            // for (int i = 0; i < size; i++)
            //    _radarcol[i] = radarcol.ReadUShort();

            _radarcol = radarcol.ReadArray<ushort>(size);
        }


        private static readonly byte[] _table = new byte[32]
        {
            0x00, 0x08, 0x10, 0x18, 0x20, 0x29, 0x31, 0x39,
            0x41, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x73, 0x7B,
            0x83, 0x8B, 0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBD,
            0xC5, 0xCD, 0xD5, 0xDE, 0xE6, 0xEE, 0xF6, 0xFF
        };

        public static void CreateHuesPalette()
        {
            _palette = new FloatHues[_huesCount];
            int entrycount = _huesCount / 8;
            for (int i = 0; i < entrycount; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int idx = (i * 8) + j;

                    _palette[idx].Palette = new float[32 * 3];

                    for (int h = 0; h < 32; h++)
                    {
                        int idx1 = h * 3;
                        ushort c = _huesRange[i].Entries[j].ColorTable[h];
                        _palette[idx].Palette[idx1] = (((c >> 10) & 0x1F) / 31.0f);
                        _palette[idx].Palette[idx1 + 1] = (((c >> 5) & 0x1F) / 31.0f);
                        _palette[idx].Palette[idx1 + 2] = ((c & 0x1F) / 31.0f);
                    }
                }
            }
        }

        public static unsafe void SetHuesBlock(int index, IntPtr ptr)
        {
            VerdataHuesGroup group = Marshal.PtrToStructure<VerdataHuesGroup>((IntPtr)ptr);
            SetHuesBlock(index, group);
        }

        public static void SetHuesBlock(int index, VerdataHuesGroup group)
        {
            if (index < 0 || index >= _huesCount)
                return;

            _huesRange[index].Header = group.Header;
            for (int i = 0; i < 8; i++)
            {
                _huesRange[index].Entries[i].ColorTable = group.Entries[i].ColorTable;
            }
        }

        public static uint Color16To32(ushort c)
            => (uint)(_table[(c >> 10) & 0x1F] |
                     (_table[(c >> 5) & 0x1F] << 8) |
                     (_table[c & 0x1F] << 16));

        public static ushort Color32To16(int c)
            => (ushort)((((c & 0xFF) * 32) / 256) |
                       (((((c >> 16) & 0xff) * 32) / 256) << 10) |
                       (((((c >> 8) & 0xff) * 32) / 256) << 5));

        public static ushort ConvertToGray(ushort c) 
            => (ushort)(((c & 0x1F) * 299 + ((c >> 5) & 0x1F) * 587 + ((c >> 10) & 0x1F) * 114) / 1000);

        public static ushort GetColor16(ushort c, ushort color)
        {
            if (color != 0 && color < _huesCount)
            {
                color--;
                int g = color / 8;
                int e = color % 8;

                return _huesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F];
            }
            return c;
        }

        public static uint GetPolygoneColor(ushort c, ushort color)
        {
            if (color != 0 && color < _huesCount)
            {
                color--;
                int g = color / 8;
                int e = color % 8;

                return Color16To32(_huesRange[g].Entries[e].ColorTable[c]);
            }
            return 0xFF010101;
        }

        public static uint GetUnicodeFontColor(ushort c, ushort color)
        {
            if (color != 0 && color < _huesCount)
            {
                color--;
                int g = color / 8;
                int e = color % 8;

                return _huesRange[g].Entries[e].ColorTable[8];
            }
            return Color16To32(c);
        }

        public static uint GetColor(ushort c, ushort color)
        {
            if (color != 0 && color < _huesCount)
            {
                color--;
                int g = color / 8;
                int e = color % 8;

                return Color16To32(_huesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F]);
            }
            return Color16To32(c);
        }

        public static uint GetPartialHueColor(ushort c, ushort color)
        {
            if (color != 0 && color < _huesCount)
            {
                color--;
                int g = color / 8;
                int e = color % 8;

                uint cl = Color16To32(c);

                if (GetR(cl) == GetG(cl) && GetB(cl) == GetG(cl))
                    return Color16To32(_huesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F]);

                return cl;
            }
            return Color16To32(c);
        }

        public static ushort GetRadarColorData(int c)
            => c < _radarcol.Length ? _radarcol[c] : (ushort)0;

        private static byte GetR(uint rgb) => LOBYTE(rgb);
        private static byte GetG(uint rgb) => LOBYTE(rgb >> 8);
        private static byte GetB(uint rgb) => LOBYTE(rgb >> 16);

        private static byte LOBYTE(uint b) => (byte)(b & 0xff);
    }



    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HuesBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public ushort[] ColorTable;
        public ushort TableStart;
        public ushort TableEnd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public char[] Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HuesGroup
    {
        public uint Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public HuesBlock[] Entries;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VerdataHuesBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public ushort[] ColorTable;
        public ushort TableStart;
        public ushort TableEnd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public char[] Name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public ushort[] Unk;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VerdataHuesGroup
    {
        public uint Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public HuesBlock[] Entries;
    }

    public struct FloatHues
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public float[] Palette;
    }
}
