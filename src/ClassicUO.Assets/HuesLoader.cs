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

        public FloatHues[] Palette { get; private set; }

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
                        HuesRange[i] = Marshal.PtrToStructure<HuesGroup>((IntPtr) (addr + (ulong) (i * groupSize)));
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

                    Hues.Initialize();
                }
            );
        }

        public float[] CreateHuesPalette()
        {
            float[] p = new float[32 * 3 * HuesCount];

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


    public static class Hues
    {
        private static int[] _header;

        public static Hue[] List { get; private set; }

        static Hues()
        {
            Initialize();
        }

        /// <summary>
        /// Reads hues.mul and fills <see cref="List"/>
        /// </summary>
        public static void Initialize()
        {
            string path = UOFileManager.GetUOFilePath("hues.mul");
            FileSystemHelper.EnsureFileExists(path);

            int index = 0;

            const int maxHueCount = 3000;
            List = new Hue[maxHueCount];

            if (path != null)
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int blockCount = (int)fs.Length / 708;

                    if (blockCount > 375)
                    {
                        blockCount = 375;
                    }

                    _header = new int[blockCount];
                    int structSize = Marshal.SizeOf(typeof(HueDataMul));
                    var buffer = new byte[blockCount * (4 + (8 * structSize))];
                    GCHandle gc = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        fs.Read(buffer, 0, buffer.Length);
                        long currentPos = 0;

                        for (int i = 0; i < blockCount; ++i) { 
                            var ptrHeader = new IntPtr((long)gc.AddrOfPinnedObject() + currentPos);
                            currentPos += 4;
                            _header[i] = (int)Marshal.PtrToStructure(ptrHeader, typeof(int));

                            for (int j = 0; j < 8; ++j, ++index)
                            {
                                var ptr = new IntPtr((long)gc.AddrOfPinnedObject() + currentPos);
                                currentPos += structSize;
                                var cur = (HueDataMul)Marshal.PtrToStructure(ptr, typeof(HueDataMul));
                                List[index] = new Hue(index, cur);
                            }
                        }
                    }
                    finally
                    {
                        gc.Free();
                    }
                }
            }

            for (; index < List.Length; ++index)
            {
                List[index] = new Hue(index);
            }
        }

        /// <summary>
        /// Returns <see cref="Hue"/>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Hue GetHue(int index)
        {
            index &= 0x3FFF;

            if (index >= 0 && index < 3000)
            {
                return List[index];
            }

            return List[0];
        }

        /// <summary>
        /// Converts RGB value to Hue color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static ushort ColorToHue(Color color)
        {
            const double scale = 31.0 / 255;

            ushort origRed = color.R;
            var newRed = (ushort)(origRed * scale);
            if (newRed == 0 && origRed != 0)
            {
                newRed = 1;
            }

            ushort origGreen = color.G;
            var newGreen = (ushort)(origGreen * scale);
            if (newGreen == 0 && origGreen != 0)
            {
                newGreen = 1;
            }

            ushort origBlue = color.B;
            var newBlue = (ushort)(origBlue * scale);
            if (newBlue == 0 && origBlue != 0)
            {
                newBlue = 1;
            }

            return (ushort)((newRed << 10) | (newGreen << 5) | newBlue);
        }

        public static int HueToColorR(ushort hue)
        {
            return ((hue & 0x7c00) >> 10) * (255 / 31);
        }

        public static int HueToColorG(ushort hue)
        {
            return ((hue & 0x3e0) >> 5) * (255 / 31);
        }

        public static int HueToColorB(ushort hue)
        {
            return (hue & 0x1f) * (255 / 31);
        }
    }

    public sealed class Hue
    {
        public int Index { get; }
        public ushort[] Colors { get; }
        public string Name { get; set; }
        public ushort TableStart { get; set; }
        public ushort TableEnd { get; set; }

        public Hue(int index)
        {
            Name = "Null";
            Index = index;
            Colors = new ushort[32];
            TableStart = 0;
            TableEnd = 0;
        }

        public Color GetColor(int index)
        {
            return HueToColor(Colors[index]);
        }

        /// <summary>
        /// Converts Hue color to RGB color
        /// </summary>
        /// <param name="hue"></param>
        private static Color HueToColor(ushort hue)
        {
            const int scale = 255 / 31;

            return new Color(((hue & 0x7c00) >> 10) * scale,
                ((hue & 0x3e0) >> 5) * scale,
                (hue & 0x1f) * scale);
        }

        private static readonly byte[] _stringBuffer = new byte[20];

        public Hue(int index, BinaryReader bin)
        {
            Index = index;
            Colors = new ushort[32];

            byte[] buffer = bin.ReadBytes(88);
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    var buf = (ushort*)bufferPtr;
                    for (int i = 0; i < 32; ++i)
                    {
                        Colors[i] = *buf++;
                    }

                    TableStart = *buf++;
                    TableEnd = *buf++;

                    var stringBuffer = (byte*)buf;
                    int count;
                    for (count = 0; count < 20 && *stringBuffer != 0; ++count)
                    {
                        _stringBuffer[count] = *stringBuffer++;
                    }

                    Name = Encoding.ASCII.GetString(_stringBuffer, 0, count);
                    Name = Name.Replace("\n", " ");
                }
            }
        }

        public Hue(int index, HueDataMul mulStruct)
        {
            Index = index;
            Colors = new ushort[32];
            for (int i = 0; i < 32; ++i)
            {
                Colors[i] = mulStruct.colors[i];
            }

            TableStart = mulStruct.tableStart;
            TableEnd = mulStruct.tableEnd;

            Name = index.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct HueDataMul
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly ushort[] colors;
        public readonly ushort tableStart;
        public readonly ushort tableEnd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly byte[] name;
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