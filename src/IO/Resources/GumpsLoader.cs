#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using System.IO;
using System.Runtime.InteropServices;

using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO.Resources
{
    internal class GumpsLoader : ResourceLoader<SpriteTexture>
    {
        private UOFile _file;
        //private readonly List<uint> _usedIndex = new List<uint>();

        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "gumpartLegacyMUL.uop");

            if (File.Exists(path))
            {
                _file = new UOFileUop(path, ".tga", Constants.MAX_GUMP_DATA_INDEX_COUNT, true);
                FileManager.UseUOPGumps = true;
            }
            else
            {
                path = Path.Combine(FileManager.UoFolderPath, "Gumpart.mul");
                string pathidx = Path.Combine(FileManager.UoFolderPath, "Gumpidx.mul");

                if (File.Exists(path) && File.Exists(pathidx))
                    _file = new UOFileMul(path, pathidx, Constants.MAX_GUMP_DATA_INDEX_COUNT, 12);
                FileManager.UseUOPGumps = false;
            }

            string pathdef = Path.Combine(FileManager.UoFolderPath, "gump.def");

            if (!File.Exists(pathdef))
                return;

            using (DefReader defReader = new DefReader(pathdef, 3))
            {
                while (defReader.Next())
                {
                    int ingump = defReader.ReadInt();

                    if (ingump < 0 || ingump >= Constants.MAX_GUMP_DATA_INDEX_COUNT ||
                        ingump >= _file.Entries.Length ||
                        _file.Entries[ingump].Length > 0)
                        continue;

                    int[] group = defReader.ReadGroup();

                    for (int i = 0; i < group.Length; i++)
                    {
                        int checkIndex = group[i];

                        if (checkIndex < 0 || checkIndex >= Constants.MAX_GUMP_DATA_INDEX_COUNT || checkIndex >= _file.Entries.Length ||
                            _file.Entries[checkIndex].Length <= 0)
                            continue;

                        _file.Entries[ingump] = _file.Entries[checkIndex];

                        break;
                    }
                }
            }
        }

        public override SpriteTexture GetTexture(uint g)
        {
            if (!ResourceDictionary.TryGetValue(g, out SpriteTexture texture) || texture.IsDisposed)
            {
                ushort[] pixels = GetGumpPixels(g, out int w, out int h);

                if (pixels == null || pixels.Length == 0)
                    return null;

                texture = new SpriteTexture(w, h, false);
                texture.SetDataHitMap16(pixels);
                ResourceDictionary.Add(g, texture);
            }

            return texture;
        }

        public override void CleanResources()
        {
            //for (int i = 0; i < _usedIndex.Count; i++)
            //{
            //    uint g = _usedIndex[i];
            //    SpriteTexture texture = ResourceDictionary[g];
            //    texture.Dispose();
            //    _usedIndex.RemoveAt(i--);
            //    ResourceDictionary.Remove(g);
            //}

            //int count = 0;

            //long ticks = Engine.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            ////foreach (SpriteTexture t in ResourceDictionary.Values.Where(s => s.Ticks < ticks).Take(20).)
            ////{
            ////    t.Dispose();
            ////}

            //ResourceDictionary
            //                  .Where(s => s.Value.Ticks < ticks)
            //                  .Take(Constants.MAX_GUMP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
            //                  .ToList()
            //                  .ForEach(s => ResourceDictionary.Remove(s.Key));
        }

        //public void ClearUnusedTextures()
        //{
        //    long ticks = Engine.Ticks - Constants.CLEAR_TEXTURES_DELAY;

        //    ResourceDictionary
        //       .Where(s => s.Value.Ticks < ticks)
        //       .Take(Constants.MAX_GUMP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
        //       .ToList()
        //       .ForEach(s =>
        //        {
        //            s.Value.Dispose();
        //            ResourceDictionary.Remove(s.Key);
        //        });

        //    //int count = 0;
        //    //long ticks = Engine.Ticks - Constants.CLEAR_TEXTURES_DELAY;

        //    //for (int i = 0; i < _usedIndex.Count; i++)
        //    //{
        //    //    uint g = _usedIndex[i];
        //    //    SpriteTexture texture = ResourceDictionary[g];

        //    //    if (texture.Ticks < ticks)
        //    //    {
        //    //        texture.Dispose();
        //    //        _usedIndex.RemoveAt(i--);
        //    //        ResourceDictionary.Remove(g);

        //    //        if (++count >= Constants.MAX_GUMP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
        //    //            break;
        //    //    }
        //    //}
        //}


        public unsafe ushort[] GetGumpPixels(uint index, out int width, out int height)
        {
            (int length, int extra, bool patcher) = _file.SeekByEntryIndex((int) index);

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

                for (int i = 0; i < gsize; i++)
                {
                    ushort val = gmul[i].Value;
                    ushort hue = (ushort) ((val != 0 ? 0x8000 : 0) | val);

                    int count = gmul[i].Run;

                    //byte a = (byte)(((val & 0x8000) >> 0) << 0);
                    //byte r = (byte)(((val & 0x7C00) >> 0) << 0);
                    //byte g = (byte)(((val & 0x3E0) >> 0) << 0);
                    //byte b = (byte)(((val & 0x1F) >> 0) << 0);

                    //byte r = (byte) (( (val >> 10) ) );
                    //byte g = (byte) (( (val >> 5)  ) );
                    //byte b = (byte) (( (val >> 0)  ) );
                    //byte a = (byte) (( (val >> 15) ) );


                    //if (b == 8)
                    //{
                    //    hue = (ushort)((
                    //                       (a << 15) |
                    //                       (r << 10) |
                    //                       (g << 5)  |
                    //                       b)
                    //                   );
                    //    hue |= 0x8000;
                    //}

                    for (int j = 0; j < count; j++)
                        pixels[pos++] = hue;
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