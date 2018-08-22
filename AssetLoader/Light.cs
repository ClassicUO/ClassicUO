using System.IO;

namespace ClassicUO.AssetsLoader
{
    public static class Light // is it useful?
    {
        public const int LIGHT_COUNT = 100;
        private static UOFileMul _file;

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "light.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "lightidx.mul");

            if (!File.Exists(path) || !File.Exists(pathidx))
                throw new FileNotFoundException();

            _file = new UOFileMul(path, pathidx, LIGHT_COUNT);
        }

        public static ushort[] GetLight(int idx, out int width, out int height)
        {
            (int length, int extra, bool patched) = _file.SeekByEntryIndex(idx);

            width = extra & 0xFFFF;
            height = (extra >> 16) & 0xFFFF;

            ushort[] pixels = new ushort[width * height];

            for (int i = 0; i < height; i++)
            {
                int pos = i * width;
                for (int j = 0; j < width; j++)
                {
                    ushort val = _file.ReadUShort();
                    val = (ushort)((val << 10) | (val << 5) | val);
                    pixels[pos + j] = (ushort)((val > 0 ? 0x8000 : 0) | val);
                }
            }

            return pixels;
        }
    }
}