using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    class HuesLoader : ResourceLoader
    {
        public HuesGroup[] HuesRange { get; private set; }

        public int HuesCount { get; private set; }

        public FloatHues[] Palette { get; private set; }

        public ushort[] RadarCol { get; private set; }

        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "hues.mul");

            if (!File.Exists(path))
                throw new FileNotFoundException();
            UOFileMul file = new UOFileMul(path, false);
            int groupSize = Marshal.SizeOf<HuesGroup>();
            int entrycount = (int)file.Length / groupSize;
            HuesCount = entrycount * 8;
            HuesRange = new HuesGroup[entrycount];
            ulong addr = (ulong)file.StartAddress;

            for (int i = 0; i < entrycount; i++)
                HuesRange[i] = Marshal.PtrToStructure<HuesGroup>((IntPtr)(addr + (ulong)(i * groupSize)));

            path = Path.Combine(FileManager.UoFolderPath, "radarcol.mul");

            if (!File.Exists(path))
                throw new FileNotFoundException();
            UOFileMul radarcol = new UOFileMul(path, false);
            RadarCol = radarcol.ReadArray<ushort>((int)radarcol.Length >> 1);
            file.Dispose();
            radarcol.Dispose();
        }

        protected override void CleanResources()
        {
            throw new NotImplementedException();
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

            for (int r = 0; r < len; r++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        int idx = r * 8 * 32 + y * 32 + x;
                        hues[idx] = HuesHelper.Color16To32(HuesRange[r].Entries[y].ColorTable[x]);
                    }
                }
            }

            return hues;
        }

        public float[] GetColorForShader(ushort color)
        {
            if (color != 0)
            {
                if (color >= HuesCount)
                {
                    color %= (ushort)HuesCount;

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
                //(byte R, byte G, byte B, byte A) = GetBGRA(cl);

                if (R == G && B == G)
                    return HuesHelper.Color16To32(HuesRange[g].Entries[e].ColorTable[(c >> 10) & 0x1F]);

                return cl;
            }

            return HuesHelper.Color16To32(c);
        }

        public ushort GetRadarColorData(int c)
        {
            return c < RadarCol.Length ? RadarCol[c] : (ushort)0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct HuesBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly ushort[] ColorTable;
        public readonly ushort TableStart;
        public readonly ushort TableEnd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly char[] Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct HuesGroup
    {
        public readonly uint Header;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly HuesBlock[] Entries;
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
