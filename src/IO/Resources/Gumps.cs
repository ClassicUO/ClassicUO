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
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO.Resources
{
    public static class Gumps
    {
        public const int GUMP_COUNT = 0x10000;
        private static UOFile _file;
        private static readonly List<int> _usedIndex = new List<int>();
        private static readonly Dictionary<int, SpriteTexture> _gumpDictionary = new Dictionary<int, SpriteTexture>();

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "gumpartLegacyMUL.uop");

            if (File.Exists(path))
            {
                _file = new UOFileUop(path, ".tga", GUMP_COUNT, true);
                FileManager.UseUOPGumps = true;
            }
            else
            {
                path = Path.Combine(FileManager.UoFolderPath, "Gumpart.mul");
                string pathidx = Path.Combine(FileManager.UoFolderPath, "Gumpidx.mul");
                if (File.Exists(path) && File.Exists(pathidx)) _file = new UOFileMul(path, pathidx, GUMP_COUNT, 12);
                FileManager.UseUOPGumps = false;
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

                    if (defs.Length < 3)
                        continue;
                    int ingump = int.Parse(defs[0]);

                    if (ingump < 0 || ingump >= GUMP_COUNT || _file.Entries[ingump].DecompressedLength != 0)
                        continue;
                    int outgump = int.Parse(defs[1].Replace("{", string.Empty).Replace("}", string.Empty));

                    if (outgump < 0 || outgump >= GUMP_COUNT || _file.Entries[outgump].DecompressedLength == 0)
                        continue;
                    int outhue = int.Parse(defs[2]);
                    _file.Entries[ingump] = _file.Entries[outgump];
                }
            }
        }

        public static SpriteTexture GetGumpTexture(ushort g)
        {
            if (!_gumpDictionary.TryGetValue(g, out SpriteTexture texture) || texture.IsDisposed)
            {
                ushort[] pixels = GetGumpPixels(g, out int w, out int h);

                if (pixels == null || pixels.Length <= 0)
                    return null;
                texture = new SpriteTexture(w, h, false);
                texture.SetDataHitMap16(pixels);
                _usedIndex.Add(g);
                _gumpDictionary.Add(g, texture);
            }
            return texture;
        }

        public static void ClearUnusedTextures()
        {
            int count = 0;
            long ticks = Engine.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            for (int i = 0; i < _usedIndex.Count; i++)
            {
                int g = _usedIndex[i];
                SpriteTexture texture = _gumpDictionary[g];

                if (texture.Ticks < ticks)
                {
                    texture.Dispose();
                    _usedIndex.RemoveAt(i--);
                    _gumpDictionary.Remove(g);

                    if (++count >= Constants.MAX_GUMP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
                        break;
                }
            }
        }

        public static void Clear()
        {
            for (int i = 0; i < _usedIndex.Count; i++)
            {
                int g = _usedIndex[i];
                SpriteTexture texture = _gumpDictionary[g]; 
                texture.Dispose();
                _usedIndex.RemoveAt(i--);
                _gumpDictionary.Remove(g);                
            }
        }


        public static unsafe ushort[] GetGumpPixels(int index, out int width, out int height)
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

            if (width == 0 || height == 0)
                return null;

            //width = PaddedRowWidth(16, width, 4) >> 1;
            IntPtr dataStart = _file.PositionAddress;
            ushort[] pixels = new ushort[width * height];
            int* lookuplist = (int*) dataStart;

            for (int y = 0; y < height; y++)
            {
                int gsize = 0;

                if (y < height - 1)
                    gsize = lookuplist[y + 1] - lookuplist[y];
                else
                    gsize = (length >> 2) - lookuplist[y];
                GumpBlock* gmul = (GumpBlock*) (dataStart + lookuplist[y] * 4);
                int pos = y * width;
                int x = 0;

                for (int i = 0; i < gsize; i++)
                {
                    ushort val = gmul[i].Value;
                    int count = gmul[i].Run;

                    if (val > 0)
                    {
                        for (int j = 0; j < count; j++)
                            pixels[pos + x++] = (ushort) (0x8000 | val);
                    }
                    else
                        x += count;

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