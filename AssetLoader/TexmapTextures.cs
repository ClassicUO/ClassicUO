using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ClassicUO.AssetsLoader
{
    public static class TextmapTextures
    {
        private static UOFile _file;

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "texmaps.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "texidx.mul");

            if (!File.Exists(path) || !File.Exists(pathidx))
            {
                throw new FileNotFoundException();
            }

            _file = new UOFileMul(path, pathidx, 0x4000, 10);

            /*string pathdef = Path.Combine(FileManager.UoFolderPath, "texterr.def");
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
            }*/
        }

        public static ushort[] GetTextmapTexture(ushort index, out int size)
        {
            (int length, int extra, bool patched) = _file.SeekByEntryIndex(index);

            if (length <= 0)
            {
                size = 0;
                return null;
            }

            size = extra == 0 ? 64 : 128;
            ushort[] pixels = new ushort[size * size];

            for (int i = 0; i < size; i++)
            {
                int pos = i * size;
                for (int j = 0; j < size; j++)
                {
                    pixels[pos + j] = (ushort)(0x8000 | _file.ReadUShort());
                }
            }

            return pixels;
        }
    }
}
