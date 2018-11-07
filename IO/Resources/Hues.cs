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
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.IO.Resources
{
    public static class Hues
    {
        private static readonly byte[] _table = new byte[32]
        {
            0x00, 0x08, 0x10, 0x18, 0x20, 0x29, 0x31, 0x39, 0x41, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x73, 0x7B, 0x83, 0x8B, 0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBD, 0xC5, 0xCD, 0xD5, 0xDE, 0xE6, 0xEE, 0xF6, 0xFF
        };

        public static HuesGroup[] HuesRange { get; private set; }

        public static int HuesCount { get; private set; }

        public static FloatHues[] Palette { get; private set; }

        public static ushort[] RadarCol { get; private set; }

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "hues.mul");

            if (!File.Exists(path))
                throw new FileNotFoundException();
            UOFileMul file = new UOFileMul(path, false);
            int groupSize = Marshal.SizeOf<HuesGroup>();
            int entrycount = (int) file.Length / groupSize;
            HuesCount = entrycount * 8;
            HuesRange = new HuesGroup[entrycount];
            ulong addr = (ulong) file.StartAddress;

            for (int i = 0; i < entrycount; i++)
                HuesRange[i] = Marshal.PtrToStructure<HuesGroup>((IntPtr) (addr + (ulong) (i * groupSize)));
            path = Path.Combine(FileManager.UoFolderPath, "radarcol.mul");

            if (!File.Exists(path))
                throw new FileNotFoundException();
            UOFileMul radarcol = new UOFileMul(path, false);
            RadarCol = radarcol.ReadArray<ushort>((int) radarcol.Length / 2);
            file.Dispose();
            radarcol.Dispose();
        }

        public static void CreateHuesPalette()
        {
            Palette = new FloatHues[HuesCount];
            int entrycount = HuesCount / 8;

            for (int i = 0; i < entrycount; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int idx = i * 8 + j;
                    Palette[idx].Palette = new float[32 * 3];

                    for (int h = 0; h < 32; h++)
                    {
                        int idx1 = h * 3;
                        ushort c = HuesRange[i].Entries[j].ColorTable[h];
                        Palette[idx].Palette[idx1] = ((c >> 10) & 0x1F) / 31.0f;
                        Palette[idx].Palette[idx1 + 1] = ((c >> 5) & 0x1F) / 31.0f;
                        Palette[idx].Palette[idx1 + 2] = (c & 0x1F) / 31.0f;
                    }
                }
            }
        }

        public static uint[] CreateShaderColors()
        {
            uint[] hues = new uint[32 * 2 * HuesCount];
            int len = HuesRange.Length;

            for (int r = 0; r < len; r++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        int idx = r * 8 * 32 + y * 32 + x;
                        hues[idx] = Color16To32(HuesRange[r].Entries[y].ColorTable[x]);
                    }
                }
            }

            return hues;
        }

        public static float[] GetColorForShader(ushort color)
        {
            if (color != 0)
            {
                if (color >= HuesCount)
                {
                    color %= (ushort) HuesCount;

                    if (color <= 0)
                        color = 1;
                }

                return Palette[color - 1].Palette;
            }

            return null;
        }

        //public static void SetHuesBlock(int index, IntPtr ptr)
        //{
        //    VerdataHuesGroup group = Marshal.PtrToStructure<VerdataHuesGroup>(ptr);
        //    SetHuesBlock(index, group);
        //}

        //public static void SetHuesBlock(int index, VerdataHuesGroup group)
        //{
        //    if (index < 0 || index >= HuesCount)
        //        return;

        //    HuesRange[index].Header = group.Header;
        //    for (int i = 0; i < 8; i++) HuesRange[index].Entries[i].ColorTable = group.Entries[i].ColorTable;
        //}

        public static uint Color16To32(ushort c)
        {
            return (uint) (_table[(c >> 10) & 0x1F] | (_table[(c >> 5) & 0x1F] << 8) | (_table[c & 0x1F] << 16));
        }

        public static ushort Color32To16(uint c)
        {
            return (ushort) (((c & 0xFF) * 32 / 256) | ((((c >> 16) & 0xff) * 32 / 256) << 10) | ((((c >> 8) & 0xff) * 32 / 256) << 5));
        }

        public static ushort ConvertToGray(ushort c)
        {
            return (ushort) (((c & 0x1F) * 299 + ((c >> 5) & 0x1F) * 587 + ((c >> 10) & 0x1F) * 114) / 1000);
        }

        public static ushort GetColor16(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color / 8;
                int e = color % 8;

                return HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F];
            }

            return c;
        }

        public static uint GetPolygoneColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color / 8;
                int e = color % 8;

                return Color16To32(HuesRange[g].Entries[e].ColorTable[c]);
            }

            return 0xFF010101;
        }

        public static uint GetUnicodeFontColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color / 8;
                int e = color % 8;

                return HuesRange[g].Entries[e].ColorTable[8];
            }

            return Color16To32(c);
        }

        public static uint GetColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color / 8;
                int e = color % 8;

                return Color16To32(HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F]);
            }

            return Color16To32(c);
        }

        public static uint GetPartialHueColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color / 8;
                int e = color % 8;
                uint cl = Color16To32(c);
                (byte B, byte G, byte R, byte A) = GetBGRA(cl);
                //(byte R, byte G, byte B, byte A) = GetBGRA(cl);

                if (R == G && B == G)
                    return Color16To32(HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F]);

                return cl;
            }

            return Color16To32(c);
        }

        public static ushort GetRadarColorData(int c)
        {
            return c < RadarCol.Length ? RadarCol[c] : (ushort) 0;
        }

        public static (byte, byte, byte, byte) GetBGRA(uint cl)
        {
            return ((byte) (cl & 0xFF), // B
                    (byte) ((cl >> 8) & 0xFF), // G
                    (byte) ((cl >> 16) & 0xFF), // R
                    (byte) ((cl >> 24) & 0xFF) // A
                   );
        }

        public static uint RgbaToArgb(uint rgba)
        {
            return (rgba >> 8) | (rgba << 24);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct HuesBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly ushort[] ColorTable;
        public readonly ushort TableStart;
        public readonly ushort TableEnd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly char[] Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct HuesGroup
    {
        public readonly uint Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly HuesBlock[] Entries;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct VerdataHuesBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly ushort[] ColorTable;
        public readonly ushort TableStart;
        public readonly ushort TableEnd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly char[] Name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly ushort[] Unk;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct VerdataHuesGroup
    {
        public readonly uint Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly HuesBlock[] Entries;
    }

    public struct FloatHues
    {
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public float[] Palette;
    }
}