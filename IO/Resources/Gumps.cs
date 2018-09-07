using ClassicUO.IO;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.IO.Resources
{
    public static class Gumps
    {
        public const int GUMP_COUNT = 0x10000;
        private static UOFile _file;

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "gumpartLegacyMUL.uop");
            if (File.Exists(path))
                _file = new UOFileUop(path, ".tga", GUMP_COUNT, true);
            else
            {
                path = Path.Combine(FileManager.UoFolderPath, "Gumpart.mul");
                string pathidx = Path.Combine(FileManager.UoFolderPath, "Gumpidx.mul");

                if (File.Exists(path) && File.Exists(pathidx)) _file = new UOFileMul(path, pathidx, GUMP_COUNT, 12);
            }

            string pathdef = Path.Combine(FileManager.UoFolderPath, "gump.def");
            if (!File.Exists(pathdef))
                return;

            using (StreamReader reader = new StreamReader(File.OpenRead(pathdef)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length <= 0 || line[0] == '#')
                        continue;
                    string[] defs = line.Replace('\t', ' ').Split(' ');
                    if (defs.Length != 3)
                        continue;

                    int ingump = int.Parse(defs[0]);
                    int outgump = int.Parse(defs[1].Replace("{", string.Empty).Replace("}", string.Empty));
                    int outhue = int.Parse(defs[2]);

                    _file.Entries[ingump] = _file.Entries[outgump];
                }
            }
        }

        public static int PaddedRowWidth(int bitsPerPixel, int w, int padToNBytes)
        {
            if (padToNBytes == 0)
                throw new ArgumentOutOfRangeException("padToNBytes", "pad value must be greater than 0.");
            int padBits = 8 * padToNBytes;
            return ((w * bitsPerPixel + (padBits - 1)) / padBits) * padToNBytes;
        }

        public static unsafe Span<ushort> GetGump(int index, out int width, out int height)
        {
            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(index);

            if (extra == -1)
            {
                width = 0;
                height = 0;
                return null;
            }

            width = (extra >> 16) & 0xFFFF;
            height = extra & 0xFFFF;

            if (width <= 0 || height <= 0)
                return null;


            width = PaddedRowWidth(16, width, 4) / 2;

            IntPtr dataStart = _file.PositionAddress;

            Span<ushort> pixels = new ushort[width * height];
            int* lookuplist = (int*)dataStart;

            for (int y = 0; y < height; y++)
            {
                int gsize = 0;
                if (y < height - 1)
                    gsize = lookuplist[y + 1] - lookuplist[y];
                else
                    gsize = length / 4 - lookuplist[y];

                GumpBlock* gmul = (GumpBlock*)(dataStart + lookuplist[y] * 4);

                int pos = y * width;
                int x = 0;

                for (int i = 0; i < gsize; i++)
                {
                    ushort val = gmul[i].Value;
                    int count = gmul[i].Run;

                    if (val > 0)
                    {
                        for (int j = 0; j < count; j++)
                        {
                            pixels[pos + x++] = (ushort)(0x8000 | val);
                        }
                    }
                    else
                    {
                        x += count;
                    }

                    //ushort a = (ushort) ((val > 0 ? 0x8000 : 0) | val);
                    //int count = gmul[i].Run;
                    //for (int j = 0; j < count; j++)
                    //    pixels[pos++] = a;
                }
            }

            return pixels;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly struct GumpBlock
        {
            public readonly ushort Value;
            public readonly ushort Run;
        }
    }
}