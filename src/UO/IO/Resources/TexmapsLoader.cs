using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO.Resources
{
    class TexmapsLoader : ResourceLoader<SpriteTexture>
    {
        private UOFile _file;
        private readonly ushort[] _textmapPixels64 = new ushort[64 * 64];
        private readonly ushort[] _textmapPixels128 = new ushort[128 * 128];
        //private readonly List<uint> _usedIndex = new List<uint>();


        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "texmaps.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "texidx.mul");

            if (!File.Exists(path) || !File.Exists(pathidx))
                throw new FileNotFoundException();
            _file = new UOFileMul(path, pathidx, Constants.MAX_LAND_TEXTURES_DATA_INDEX_COUNT, 10);
            string pathdef = Path.Combine(FileManager.UoFolderPath, "TexTerr.def");

            if (!File.Exists(pathdef))
                return;

            using (DefReader defReader = new DefReader(pathdef, 2))
            {
                while (defReader.Next())
                {
                    int index = defReader.ReadInt();
                    if (index < 0 || index >= Constants.MAX_LAND_TEXTURES_DATA_INDEX_COUNT)
                        continue;

                    int[] group = defReader.ReadGroup();

                    for (int i = 0; i < group.Length; i++)
                    {
                        int checkindex = group[i];

                        if (checkindex < 0 || checkindex >= Constants.MAX_LAND_TEXTURES_DATA_INDEX_COUNT)
                            continue;
                        _file.Entries[index] = _file.Entries[checkindex];
                    }
                }
            }

            //using (StreamReader reader = new StreamReader(File.OpenRead(pathdef)))
            //{
            //    string line;

            //    while ((line = reader.ReadLine()) != null)
            //    {
            //        line = line.Trim();

            //        if (line.Length <= 0 || line[0] == '#')
            //            continue;

            //        string[] defs = line.Split(new[]
            //        {
            //            '\t', ' ', '#'
            //        }, StringSplitOptions.RemoveEmptyEntries);

            //        if (defs.Length < 2)
            //            continue;
            //        int index = int.Parse(defs[0]);

            //        if (index < 0 || index >= TEXTMAP_COUNT)
            //            continue;
            //        int first = defs[1].IndexOf("{");
            //        int last = defs[1].IndexOf("}");

            //        string[] newdef = defs[1].Substring(first + 1, last - 1).Split(new[]
            //        {
            //            ' ', ','
            //        }, StringSplitOptions.RemoveEmptyEntries);

            //        foreach (string s in newdef)
            //        {
            //            int checkindex = int.Parse(s);

            //            if (checkindex < 0 || checkindex >= TEXTMAP_COUNT)
            //                continue;
            //            _file.Entries[index] = _file.Entries[checkindex];
            //        }
            //    }
            //}
        }

        public override SpriteTexture GetTexture(uint g)
        {
            if (!ResourceDictionary.TryGetValue(g, out SpriteTexture texture) || texture.IsDisposed)
            {
                ushort[] pixels = GetTextmapTexture( (ushort) g, out int size);

                if (pixels == null || pixels.Length == 0)
                    return null;
                texture = new SpriteTexture(size, size, false);
                texture.SetData(pixels);
                //_usedIndex.Add(g);
                ResourceDictionary.Add(g, texture);
            }

            return texture;
        }

        public override void CleanResources()
        {
            throw new NotImplementedException();
        }

        //public void ClearUnusedTextures()
        //{
        //    int count = 0;
        //    long ticks = Engine.Ticks - 3000;

        //    for (int i = 0; i < _usedIndex.Count; i++)
        //    {
        //        uint g = _usedIndex[i];
        //        SpriteTexture texture = ResourceDictionary[g];

        //        if (texture.Ticks < ticks)
        //        {
        //            texture.Dispose();
        //            _usedIndex.RemoveAt(i--);
        //            ResourceDictionary.Remove(g);

        //            if (++count >= 20)
        //                break;
        //        }
        //    }
        //}

        //public void Clear()
        //{
        //    for (int i = 0; i < _usedIndex.Count; i++)
        //    {
        //        uint g = _usedIndex[i];
        //        SpriteTexture texture = ResourceDictionary[g];
        //        texture.Dispose();
        //        _usedIndex.RemoveAt(i--);
        //        ResourceDictionary.Remove(g);
        //    }
        //}

        private ushort[] GetTextmapTexture(ushort index, out int size)
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

                for (int j = 0; j < size; j++)
                    pixels[pos + j] = (ushort)(0x8000 | _file.ReadUShort());
            }

            return pixels;
        }
    }
}
