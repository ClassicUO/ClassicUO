using System.IO;

namespace ClassicUO.AssetsLoader
{
    public static class Light // is it useful?
    {
        public const int LIGHT_COUNT = 100;
        private static UOFileMul _file;

        public static void Load()
        {
            var path = Path.Combine(FileManager.UoFolderPath, "light.mul");
            var pathidx = Path.Combine(FileManager.UoFolderPath, "lightidx.mul");

            if (!File.Exists(path) || !File.Exists(pathidx))
                throw new FileNotFoundException();

            _file = new UOFileMul(path, pathidx, LIGHT_COUNT);
        }

        public static ushort[] GetLight(int idx, out int width, out int height)
        {
            var (length, extra, patched) = _file.SeekByEntryIndex(idx);

            width = extra & 0xFFFF;
            height = (extra >> 16) & 0xFFFF;

            var pixels = new ushort[width * height];

            for (var i = 0; i < height; i++)
            {
                var pos = i * width;
                for (var j = 0; j < width; j++)
                {
                    var val = _file.ReadUShort();
                    val = (ushort) ((val << 10) | (val << 5) | val);
                    pixels[pos + j] = (ushort) ((val > 0 ? 0x8000 : 0) | val);
                }
            }

            return pixels;
        }
    }
}