﻿#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class TexmapsLoader : UOFileLoader<UOTexture>
    {
        private static TexmapsLoader _instance;
        private UOFile _file;

        private TexmapsLoader(int count) : base(count)
        {
        }

        public static TexmapsLoader Instance =>
            _instance ?? (_instance = new TexmapsLoader(Constants.MAX_LAND_TEXTURES_DATA_INDEX_COUNT));

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("texmaps.mul");
                    string pathidx = UOFileManager.GetUOFilePath("texidx.mul");

                    FileSystemHelper.EnsureFileExists(path);
                    FileSystemHelper.EnsureFileExists(pathidx);

                    _file = new UOFileMul(path, pathidx, Constants.MAX_LAND_TEXTURES_DATA_INDEX_COUNT, 10);
                    _file.FillEntries(ref Entries);
                    string pathdef = UOFileManager.GetUOFilePath("TexTerr.def");

                    if (!File.Exists(pathdef))
                    {
                        return;
                    }

                    using (DefReader defReader = new DefReader(pathdef))
                    {
                        while (defReader.Next())
                        {
                            int index = defReader.ReadInt();

                            if (index < 0 || index >= Constants.MAX_LAND_TEXTURES_DATA_INDEX_COUNT)
                            {
                                continue;
                            }

                            int[] group = defReader.ReadGroup();

                            if (group == null)
                            {
                                continue;
                            }

                            for (int i = 0; i < group.Length; i++)
                            {
                                int checkindex = group[i];

                                if (checkindex < 0 || checkindex >= Constants.MAX_LAND_TEXTURES_DATA_INDEX_COUNT)
                                {
                                    continue;
                                }

                                Entries[index] = Entries[checkindex];
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
            );
        }

        public override UOTexture GetTexture(uint g)
        {
            if (g >= Resources.Length)
            {
                return null;
            }

            ref UOTexture texture = ref Resources[g];

            if (texture == null || texture.IsDisposed)
            {
                ReadTexmapTexture(ref texture, (ushort) g);

                if (texture != null)
                {
                    SaveId(g);
                }
            }
            else
            {
                texture.Ticks = Time.Ticks;
            }

            return texture;
        }

        private unsafe void ReadTexmapTexture(ref UOTexture texture, ushort index)
        {
            ref UOFileIndex entry = ref GetValidRefEntry(index);

            if (entry.Length <= 0)
            {
                texture = null;

                return;
            }

            int size = entry.Width == 0 && entry.Height == 0 ? 64 : 128;
            int size_pot = size * size;

            uint* data = stackalloc uint[size_pot];

            _file.Seek(entry.Offset);

            for (int i = 0; i < size; ++i)
            {
                int pos = i * size;

                for (int j = 0; j < size; ++j)
                {
                    data[pos + j] = HuesHelper.Color16To32(_file.ReadUShort()) | 0xFF_00_00_00;
                }
            }

            texture = new UOTexture(size, size);
            // we don't need to store the data[] pointer because
            // land is always hoverable
            texture.SetDataPointerEXT(0, null, (IntPtr) data, size_pot * sizeof(uint));
        }
    }
}