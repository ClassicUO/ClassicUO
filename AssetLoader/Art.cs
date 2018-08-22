using System.IO;

namespace ClassicUO.AssetsLoader
{
    /*
     *      TODO: 
     *          - use Span<T> (retarget to netcore2.1) to read pixels and other array stuffs to improve performances
     */

    public static class Art
    {
        public const int ART_COUNT = 0x10000;
        private static UOFile _file;

        private static readonly ushort[] _landArray = new ushort[44 * 44];


        public static void Load()
        {
            string filepath = Path.Combine(FileManager.UoFolderPath, "artLegacyMUL.uop");

            if (File.Exists(filepath))
                _file = new UOFileUop(filepath, ".tga", ART_COUNT);
            else
            {
                filepath = Path.Combine(FileManager.UoFolderPath, "art.mul");
                string idxpath = Path.Combine(FileManager.UoFolderPath, "artidx.mul");
                if (File.Exists(filepath) && File.Exists(idxpath))
                    _file = new UOFileMul(filepath, idxpath, ART_COUNT);
            }
        }

        public static unsafe ushort[] ReadStaticArt(ushort graphic, out short width, out short height)
        {
            graphic &= FileManager.GraphicMask;

            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic + 0x4000);

            _file.Skip(4);

            width = _file.ReadShort();
            height = _file.ReadShort();

            if (width <= 0 || height <= 0)
                return new ushort[0];

            ushort[] pixels = new ushort[width * height];

            ushort* ptr = (ushort*)_file.PositionAddress;

            ushort* lineoffsets = ptr;
            byte* datastart = (byte*)ptr + height * 2;

            int x = 0;
            int y = 0;
            ushort xoffs = 0;
            ushort run = 0;

            ptr = (ushort*)(datastart + lineoffsets[0] * 2);

            while (y < height)
            {
                xoffs = *ptr++;
                run = *ptr++;

                if (xoffs + run >= 2048)
                {
                    pixels = new ushort[width * height];
                    return pixels;
                }

                if (xoffs + run != 0)
                {
                    x += xoffs;
                    int pos = y * width + x;
                    for (int j = 0; j < run; j++)
                    {
                        ushort val = *ptr++;
                        if (val > 0)
                            val = (ushort)(0x8000 | val);
                        pixels[pos++] = val;
                    }

                    x += run;
                }
                else
                {
                    x = 0;
                    y++;
                    ptr = (ushort*)(datastart + lineoffsets[y] * 2);
                }
            }

            if (graphic >= 0x2053 && graphic <= 0x2062 || graphic >= 0x206A && graphic <= 0x2079)
            {
                for (int i = 0; i < width; i++)
                {
                    pixels[i] = 0;
                    pixels[(height - 1) * width + i] = 0;
                }

                for (int i = 0; i < height; i++)
                {
                    pixels[i * width] = 0;
                    pixels[i * width + width - 1] = 0;
                }
            }

            return pixels;
        }

        public static ushort[] ReadLandArt(ushort graphic)
        {
            graphic &= FileManager.GraphicMask;

            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic);

            for (int i = 0; i < 22; i++)
            {
                int start = 22 - (i + 1);
                int pos = i * 44 + start;
                int end = start + (i + 1) * 2;

                for (int j = start; j < end; j++)
                {
                    ushort val = _file.ReadUShort();
                    if (val > 0)
                        val = (ushort)(0x8000 | val);

                    _landArray[pos++] = val;
                }
            }

            for (int i = 0; i < 22; i++)
            {
                int pos = (i + 22) * 44 + i;
                int end = i + (22 - i) * 2;

                for (int j = i; j < end; j++)
                {
                    ushort val = _file.ReadUShort();
                    if (val > 0)
                        val = (ushort)(0x8000 | val);

                    _landArray[pos++] = val;
                }
            }

            return _landArray;
        }
    }
}