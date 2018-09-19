#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using ClassicUO.Renderer;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.IO.Resources
{
    public static class TextmapTextures
    {
        public const int TEXTMAP_COUNT = 0x4000;
        private static UOFile _file;
        private static readonly ushort[] _textmapPixels64 = new ushort[64 * 64];
        private static readonly ushort[] _textmapPixels128 = new ushort[128 * 128];


        private static SpriteTexture[] _textmapCache;
        private static readonly List<int> _usedIndex = new List<int>();

        //private static readonly PixelPicking _picker = new PixelPicking();


        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "texmaps.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "texidx.mul");

            if (!File.Exists(path) || !File.Exists(pathidx))
                throw new FileNotFoundException();

            _file = new UOFileMul(path, pathidx, TEXTMAP_COUNT, 10);

            _textmapCache = new SpriteTexture[TEXTMAP_COUNT];

            string pathdef = Path.Combine(FileManager.UoFolderPath, "TexTerr.def");
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
                    string[] defs = line.Split(new[] { '\t', ' ', '#' }, StringSplitOptions.RemoveEmptyEntries);
                    if (defs.Length < 2)
                        continue;

                    int index = int.Parse(defs[0]);

                    if (index < 0 || index >= TEXTMAP_COUNT)
                        continue;

                    int first = defs[1].IndexOf("{");
                    int last = defs[1].IndexOf("}");

                    string[] newdef = defs[1].Substring(first + 1, last - 1).Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string s in newdef)
                    {
                        int checkindex = int.Parse(s);

                        if (checkindex < 0 || checkindex >= TEXTMAP_COUNT)
                            continue;

                        _file.Entries[index] = _file.Entries[checkindex];
                    }
                }
            }
        }

        //public static bool Contains(ushort g, int x, int y, int extra = 0)
        //     => _picker.Get(g, x, y, extra);

        //public static void Clear(ushort g) => _picker.Remove(g);

        public static unsafe SpriteTexture GetTextmapTexture(ushort g)
        {
            ref var texture = ref _textmapCache[g];
            if (texture == null || texture.IsDisposed)
            {
                var pixels = GetTextmapTexture(g, out int size);
                texture = new SpriteTexture(size, size, false);
                texture.SetData(pixels);

                _usedIndex.Add(g);

                //_picker.Set(g, size, size, pixels);
            }

            return texture;
        }

        public static void ClearUnusedTextures()
        {
            int count = 0;
            for (int i = 0; i < _usedIndex.Count; i++)
            {
                ref var texture = ref _textmapCache[_usedIndex[i]];
                if (texture == null || texture.IsDisposed)
                    _usedIndex.RemoveAt(i--);
                else if (Game.World.Ticks - texture.Ticks >= 3000)
                {
                    //_picker.Remove(_usedIndex[i]);
                    texture.Dispose();
                    texture = null;

                    _usedIndex.RemoveAt(i);
                    i--;
                    if (++count >= 5)
                        break;
                }
            }
        }

        private static ushort[] GetTextmapTexture(ushort index, out int size)
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