using ClassicUO.IO;
using System.IO;

namespace ClassicUO.IO.Resources
{
    public static class TextmapTextures
    {
        public const int TEXTMAP_COUNT = 0x4000;
        private static UOFile _file;

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "texmaps.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "texidx.mul");

            if (!File.Exists(path) || !File.Exists(pathidx)) throw new FileNotFoundException();

            _file = new UOFileMul(path, pathidx, TEXTMAP_COUNT, 10);

            string pathdef = Path.Combine(FileManager.UoFolderPath, "texterr.def");
            if (File.Exists(pathdef))
            {
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

                        int checkindex = int.Parse(defs[1].Replace("{", string.Empty).Replace("}", string.Empty));

                        
                    }
                }
            }
        }

        private static readonly ushort[] _textmapPixels64 = new ushort[64 * 64];
        private static readonly ushort[] _textmapPixels128 = new ushort[128 * 128];

        public static ushort[] GetTextmapTexture(ushort index, out int size)
        {
            (int length, int extra, bool patched) = _file.SeekByEntryIndex(index);

            if (length <= 0)
            {
                size = 0;
                return null;
            }

            ushort[] pixels;

            if (extra == 0)
            {
                size = 64;
                pixels = _textmapPixels64;
            }
            else
            {
                size = 128;
                pixels = _textmapPixels128;
            }            

            for (int i = 0; i < size; i++)
            {
                int pos = i * size;
                for (int j = 0; j < size; j++) pixels[pos + j] = (ushort)(0x8000 | _file.ReadUShort());
            }

            return pixels;
        }
    }
}