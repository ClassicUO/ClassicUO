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
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class HuesLoader : ResourceLoader
    {
        public HuesGroup[] HuesRange { get; private set; }

        public int HuesCount { get; private set; }

        public FloatHues[] Palette { get; private set; }

        public ushort[] RadarCol { get; private set; }

        public override Task Load()
        {
            return Task.Run(() =>
            {
                string path = Path.Combine(FileManager.UoFolderPath, "hues.mul");

                FileSystemHelper.EnsureFileExists(path);

                UOFileMul file = new UOFileMul(path);
                int groupSize = Marshal.SizeOf<HuesGroup>();
                int entrycount = (int) file.Length / groupSize;
                HuesCount = entrycount * 8;
                HuesRange = new HuesGroup[entrycount];
                ulong addr = (ulong) file.StartAddress;

                for (int i = 0; i < entrycount; i++)
                    HuesRange[i] = Marshal.PtrToStructure<HuesGroup>((IntPtr) (addr + (ulong) (i * groupSize)));

                path = Path.Combine(FileManager.UoFolderPath, "radarcol.mul");

                FileSystemHelper.EnsureFileExists(path);

                UOFileMul radarcol = new UOFileMul(path);
                RadarCol = radarcol.ReadArray<ushort>((int) radarcol.Length >> 1);
                file.Dispose();
                radarcol.Dispose();
            });
        }


        public override void CleanResources()
        {
            // nothing to clear
        }

        public void CreateHuesPalette()
        {
            Palette = new FloatHues[HuesCount];
            int entrycount = HuesCount >> 3;

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

        public uint[] CreateShaderColors()
        {
            uint[] hues = new uint[32 * 2 * HuesCount];
            int len = HuesRange.Length;

            int idx = 0;

            for (int r = 0; r < len; r++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 32; x++) hues[idx++] = HuesHelper.Color16To32(HuesRange[r].Entries[y].ColorTable[x]);
                }
            }

            for (int r = 0; r < len; r++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        if (x == 0)
                            hues[idx++] = HuesHelper.Color16To32(HuesRange[0].Entries[0].ColorTable[0]);
                        else
                            hues[idx++] = HuesHelper.Color16To32(HuesRange[r].Entries[y].ColorTable[x]);
                    }
                }
            }

            return hues;
        }

        //public float[] GetColorForShader(ushort color)
        //{
        //    if (color != 0)
        //    {
        //        if (color >= HuesCount)
        //        {
        //            color %= (ushort)HuesCount;

        //            if (color <= 0)
        //                color = 1;
        //        }

        //        return Palette[color - 1].Palette;
        //    }

        //    return _empty;
        //}

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

        public ushort GetColor16(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;

                return HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F];
            }

            return c;
        }

        public uint GetPolygoneColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;

                return HuesHelper.Color16To32(HuesRange[g].Entries[e].ColorTable[c]);
            }

            return 0xFF010101;
        }

        public uint GetUnicodeFontColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;

                return HuesRange[g].Entries[e].ColorTable[8];
            }

            return HuesHelper.Color16To32(c);
        }

        public uint GetColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;

                return HuesHelper.Color16To32(HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F]);
            }

            return HuesHelper.Color16To32(c);
        }

        public uint GetPartialHueColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;
                uint cl = HuesHelper.Color16To32(c);
                (byte B, byte G, byte R, byte A) = HuesHelper.GetBGRA(cl);
                //(byte R, byte G, byte B, byte A) = HuesHelper.GetBGRA(cl);

                if (R == G && B == G)
                    return HuesHelper.Color16To32(HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F]);

                return cl;
            }

            return HuesHelper.Color16To32(c);
        }

        [MethodImpl(256)]
        public ushort GetRadarColorData(int c) => c >= 0 && c < RadarCol.Length ? RadarCol[c] : (ushort) 0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct HuesBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public ushort[] ColorTable;
        public ushort TableStart;
        public ushort TableEnd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public char[] Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct HuesGroup
    {
        public uint Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public HuesBlock[] Entries;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct VerdataHuesBlock
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
    internal readonly struct VerdataHuesGroup
    {
        public readonly uint Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly HuesBlock[] Entries;
    }

    internal struct FloatHues
    {
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public float[] Palette;
    }
}