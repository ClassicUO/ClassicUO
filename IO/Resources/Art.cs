#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System.Collections.Generic;
using System.IO;

using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO.Resources
{
    public static class Art
    {
        public const int ART_COUNT = 0x10000;
        private static UOFile _file;
        //private static SpriteTexture[] _artCache;
        //private static SpriteTexture[] _landCache;
        private static readonly List<int> _usedIndex = new List<int>();
        private static readonly List<int> _usedIndexLand = new List<int>();
        private static readonly PixelPicking _picker = new PixelPicking();
        private static readonly Dictionary<Graphic, SpriteTexture> _artDictionary = new Dictionary<Graphic, SpriteTexture>();
        private static readonly Dictionary<Graphic, SpriteTexture> _landDictionary = new Dictionary<Graphic, SpriteTexture>();

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

            //_artCache = new SpriteTexture[ART_COUNT];
            //_landCache = new SpriteTexture[ART_COUNT];
        }

        public static bool Contains(ushort g, int x, int y, int extra = 0)
        {
            return _picker.Get(g, x, y, extra);
        }

        public static SpriteTexture GetStaticTexture(ushort g)
        {

            if (!_artDictionary.TryGetValue(g, out SpriteTexture texture) || texture.IsDisposed)
            {
                ushort[] pixels = ReadStaticArt(g, out short w, out short h);
                texture = new SpriteTexture(w, h, false);
                texture.SetData(pixels);
                _usedIndex.Add(g);
                _picker.Set(g, w, h, pixels);
                _artDictionary.Add(g, texture);
            }

            //ref SpriteTexture texture = ref _artCache[g];

            //if (texture == null || texture.IsDisposed)
            //{
            //    ushort[] pixels = ReadStaticArt(g, out short w, out short h);
            //    texture = new SpriteTexture(w, h, false);
            //    texture.SetData(pixels);
            //    _usedIndex.Add(g);
            //    _picker.Set(g, w, h, pixels);
            //}

            return texture;
        }

        public static SpriteTexture GetLandTexture(ushort g)
        {
            if (!_landDictionary.TryGetValue(g, out SpriteTexture texture) || texture.IsDisposed)
            {
                const int SIZE = 44;
                ushort[] pixels = ReadLandArt(g);
                texture = new SpriteTexture(SIZE, SIZE, false);
                texture.SetData(pixels);
                _usedIndexLand.Add(g);
                _picker.Set(g, SIZE, SIZE, pixels);

                _landDictionary.Add(g, texture);
            }

            //ref SpriteTexture texture = ref _landCache[g];

            //if (texture == null || texture.IsDisposed)
            //{
            //    const int SIZE = 44;
            //    ushort[] pixels = ReadLandArt(g);
            //    texture = new SpriteTexture(SIZE, SIZE, false);
            //    texture.SetData(pixels);
            //    _usedIndexLand.Add(g);
            //    _picker.Set(g, SIZE, SIZE, pixels);
            //}

            return texture;
        }

        public static void ClearUnusedTextures()
        {
            int count = 0;

            for (int i = 0; i < _usedIndex.Count; i++)
            {
                //ref SpriteTexture texture = ref _artCache[_usedIndex[i]];
                Graphic g = (Graphic) _usedIndex[i];
                SpriteTexture texture = _artDictionary[g];

                if (texture == null || texture.IsDisposed)
                    _usedIndex.RemoveAt(i--);
                else if (CoreGame.Ticks - texture.Ticks >= 3000)
                {
                    texture.Dispose();
                    //texture = null;
                    _usedIndex.RemoveAt(i--);
                    _artDictionary.Remove(g);

                    if (++count >= 5)
                        break;
                }
            }

            count = 0;

            for (int i = 0; i < _usedIndexLand.Count; i++)
            {
                //ref SpriteTexture texture = ref _landCache[_usedIndexLand[i]];
                Graphic g = (Graphic)_usedIndexLand[i];
                SpriteTexture texture = _landDictionary[g];

                if (texture == null || texture.IsDisposed)
                    _usedIndexLand.RemoveAt(i--);
                else if (CoreGame.Ticks - texture.Ticks >= 3000)
                {
                    texture.Dispose();
                  //  texture = null;
                    _usedIndexLand.RemoveAt(i--);
                    _landDictionary.Remove(g);
                    if (++count >= 5)
                        break;
                }
            }
        }

        private static unsafe ushort[] ReadStaticArt(ushort graphic, out short width, out short height)
        {
            graphic &= FileManager.GraphicMask;
            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic + 0x4000);
            _file.Skip(4);
            width = _file.ReadShort();
            height = _file.ReadShort();

            if (width == 0 || height == 0)
                return new ushort[0];
            ushort[] pixels = new ushort[width * height];
            ushort* ptr = (ushort*) _file.PositionAddress;
            ushort* lineoffsets = ptr;
            byte* datastart = (byte*) ptr + height * 2;
            int x = 0;
            int y = 0;
            ptr = (ushort*) (datastart + lineoffsets[0] * 2);

            while (y < height)
            {
                ushort xoffs = *ptr++;
                ushort run = *ptr++;

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
                            val = (ushort) (0x8000 | val);
                        pixels[pos++] = val;
                    }

                    x += run;
                }
                else
                {
                    x = 0;
                    y++;
                    ptr = (ushort*) (datastart + lineoffsets[y] * 2);
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

        private static unsafe ushort[] ReadLandArt(ushort graphic)
        {
            graphic &= FileManager.GraphicMask;
            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic);

            ushort[] pixels = new ushort[44 * 44];

            for (int i = 0; i < 22; i++)
            {
                int start = 22 - (i + 1);
                int pos = i * 44 + start;
                int end = start + (i + 1) * 2;

                for (int j = start; j < end; j++)
                {
                    ushort val = _file.ReadUShort();

                    if (val > 0)
                        val = (ushort) (0x8000 | val);
                    pixels[pos++] = val;
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
                        val = (ushort) (0x8000 | val);
                    pixels[pos++] = val;
                }
            }

            return pixels;
        }
    }
}