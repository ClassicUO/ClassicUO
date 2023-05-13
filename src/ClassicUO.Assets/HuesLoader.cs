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

using ClassicUO.IO;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class HuesLoader : UOFileLoader
    {
        private static HuesLoader _instance;

        private HuesLoader()
        {
        }

        public static HuesLoader Instance => _instance ?? (_instance = new HuesLoader());

        public HuesGroup[] HuesRange { get; private set; }

        public int HuesCount { get; private set; }

        public ushort[] RadarCol { get; private set; }

        public override unsafe Task Load()
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
                        HuesRange[i] = Marshal.PtrToStructure<HuesGroup>((IntPtr)(addr + (ulong) (i * groupSize)));
                    }

                    path = UOFileManager.GetUOFilePath("radarcol.mul");

                    FileSystemHelper.EnsureFileExists(path);

                    UOFileMul radarcol = new UOFileMul(path);
                    RadarCol = new ushort[(int)(radarcol.Length >> 1)];

                    fixed (ushort* ptr = RadarCol)
                    {
                        Unsafe.CopyBlockUnaligned((void*)(byte*)ptr, radarcol.PositionAddress.ToPointer(), (uint)radarcol.Length);
                    }
                    
                    file.Dispose();
                    radarcol.Dispose();
                }
            );
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

        /* Look up the hue and return the color for the given index. Index must be between 0 and 31.
         * The returned color is a 16 bit color in R5B5G5A1 format. */
        public ushort GetHueColorRgba5551(ushort index, ushort hue)
        {
            if (hue != 0 && hue < HuesCount)
            {
                hue -= 1;
                int g = hue >> 3;
                int e = hue % 8;

                return (ushort)(0x8000 | HuesRange[g].Entries[e].ColorTable[index]);
            }

            return 0x8000;
        }

        /* Look up the hue and return the color for the given index. Index must be between 0 and 31.
         * The returned color is a 32 bit color in R8G8B8A8 format. */
        public uint GetHueColorRgba8888(ushort index, ushort hue)
        {
            return HuesHelper.Color16To32(GetHueColorRgba5551(index, hue));
        }

        /* Apply the hue to the given gray color, returning a 16 bit color. */
        public ushort ApplyHueRgba5551(ushort gray, ushort hue)
        {
            return GetHueColorRgba5551((ushort)((gray >> 10) & 0x1F), hue);
        }

        /* Apply the hue to the given gray color, returning a 32 bit color. */
        public uint ApplyHueRgba8888(ushort gray, ushort hue)
        {
            return HuesHelper.Color16To32(ApplyHueRgba5551(gray, hue));
        }

        public uint GetPartialHueColor(ushort color, ushort hue)
        {
            uint cl = HuesHelper.Color16To32(color);
            byte R = (byte)(cl & 0xFF);
            byte G = (byte)((cl >> 8) & 0xFF);
            byte B = (byte)((cl >> 16) & 0xFF);

            if (R != G || R != B)
            {
                /* Not gray. Don't apply hue. */
                return HuesHelper.Color16To32(color);
            }

            if (hue == 0 || hue >= HuesCount)
            {
                /* Invalid hue. */
                return HuesHelper.Color16To32(color);
            }

            return ApplyHueRgba8888(color, hue);
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
        public readonly VerdataHuesBlock[] Entries;
    }

    public struct FloatHues
    {
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public float[] Palette;
    }
}