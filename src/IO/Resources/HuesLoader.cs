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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class HuesLoader : UOFileLoader
    {
        private static HuesLoader _instance;

        private HuesLoader()
        {
        }

        public static HuesLoader Instance => _instance ?? (_instance = new HuesLoader());

        public HuesGroup[] HuesRange { get; private set; }

        public int HuesCount { get; private set; }

        public FloatHues[] Palette { get; private set; }

        public ushort[] RadarCol { get; private set; }

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("hues.mul");

                    FileSystemHelper.EnsureFileExists(path);

                    UOFileMul file = new UOFileMul(path);
                    int groupSize = Marshal.SizeOf<HuesGroup>();
                    int entrycount = (int) file.Length / groupSize;
                    HuesCount = entrycount * 8;
                    HuesRange = new HuesGroup[entrycount];
                    ulong addr = (ulong) file.StartAddress;

                    for (int i = 0; i < entrycount; i++)
                    {
                        HuesRange[i] = Marshal.PtrToStructure<HuesGroup>((IntPtr) (addr + (ulong) (i * groupSize)));
                    }

                    path = UOFileManager.GetUOFilePath("radarcol.mul");

                    FileSystemHelper.EnsureFileExists(path);

                    UOFileMul radarcol = new UOFileMul(path);
                    RadarCol = radarcol.ReadArray<ushort>((int) radarcol.Length >> 1);
                    file.Dispose();
                    radarcol.Dispose();
                }
            );
        }

        public float[] CreateHuesPalette()
        {
            float[] p = new float[32 * 3 * HuesCount];

            Palette = new FloatHues[HuesCount];
            int entrycount = HuesCount >> 3;
            int iddd = 0;

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

                        p[idx * 96 + idx1 + 0] = Palette[idx].Palette[idx1];

                        p[idx * 96 + idx1 + 1] = Palette[idx].Palette[idx1 + 1];

                        p[idx * 96 + idx1 + 2] = Palette[idx].Palette[idx1 + 2];

                        //p[iddd++] = Palette[idx].Palette[idx1];
                        //p[iddd++] = Palette[idx].Palette[idx1 + 1];
                        //p[iddd++] = Palette[idx].Palette[idx1 + 2];
                    }
                }
            }

            return p;
        }

        public void CreateShaderColors(uint[] buffer)
        {
            int len = HuesRange.Length;

            int idx = 0;

            for (int r = 0; r < len; r++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        buffer[idx++] = HuesHelper.Color16To32(HuesRange[r].Entries[y].ColorTable[x]) | 0xFF_00_00_00;

                        if (idx >= buffer.Length)
                        {
                            return;
                        }
                    }
                }
            }
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

            return color != 0 ? HuesHelper.Color16To32(color) : HuesHelper.Color16To32(c);
        }

        public uint GetPartialHueColor(ushort c, ushort color)
        {
            if (color != 0 && color < HuesCount)
            {
                color -= 1;
                int g = color >> 3;
                int e = color % 8;
                uint cl = HuesHelper.Color16To32(c);

                byte R = (byte) (cl & 0xFF);
                byte G = (byte) ((cl >> 8) & 0xFF);
                byte B = (byte) ((cl >> 16) & 0xFF);

                if (R == G && R == B)
                {
                    cl = HuesHelper.Color16To32(HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F]);
                }

                return cl;
            }

            return HuesHelper.Color16To32(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetRadarColorData(int c)
        {
            if (c >= 0 && c < RadarCol.Length)
            {
                return RadarCol[c];
            }

            return 0;
        }
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
        public readonly VerdataHuesBlock[] Entries;
    }

    internal struct FloatHues
    {
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public float[] Palette;
    }
}