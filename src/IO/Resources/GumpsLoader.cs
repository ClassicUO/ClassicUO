#region license
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
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO.Resources
{
    internal class GumpsLoader : UOFileLoader<UOTexture16>
    {
        private UOFile _file;

        private GumpsLoader()
        {

        }

        private static GumpsLoader _instance;
        public static GumpsLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GumpsLoader();
                }

                return _instance;
            }
        }

        public override Task Load()
        {
            return Task.Run(() => {

                string path = UOFileManager.GetUOFilePath("gumpartLegacyMUL.uop");

                if (File.Exists(path))
                {
                    _file = new UOFileUop(path, "build/gumpartlegacymul/{0:D8}.tga", true);
                    Entries = new UOFileIndex[Constants.MAX_GUMP_DATA_INDEX_COUNT];
                    Client.UseUOPGumps = true;
                }
                else
                {
                    path = UOFileManager.GetUOFilePath("Gumpart.mul");
                    string pathidx = UOFileManager.GetUOFilePath("Gumpidx.mul");

                    if (File.Exists(path) && File.Exists(pathidx))
                    {
                        _file = new UOFileMul(path, pathidx, Constants.MAX_GUMP_DATA_INDEX_COUNT, 12);
                    }
                    Client.UseUOPGumps = false;
                }
                _file.FillEntries(ref Entries);

                string pathdef = UOFileManager.GetUOFilePath("gump.def");

                if (!File.Exists(pathdef))
                    return;

                using (DefReader defReader = new DefReader(pathdef, 3))
                {
                    while (defReader.Next())
                    {
                        int ingump = defReader.ReadInt();

                        if (ingump < 0 || ingump >= Constants.MAX_GUMP_DATA_INDEX_COUNT ||
                            ingump >= Entries.Length ||
                            Entries[ingump].Length > 0)
                            continue;

                        int[] group = defReader.ReadGroup();

                        for (int i = 0; i < group.Length; i++)
                        {
                            int checkIndex = group[i];

                            if (checkIndex < 0 || checkIndex >= Constants.MAX_GUMP_DATA_INDEX_COUNT || checkIndex >= Entries.Length ||
                                Entries[checkIndex].Length <= 0)
                                continue;

                            Entries[ingump] = Entries[checkIndex];

                            break;
                        }
                    }
                }
            });
        }

        public override UOTexture16 GetTexture(uint g)
        {
            if (!ResourceDictionary.TryGetValue(g, out UOTexture16 texture) || texture.IsDisposed)
            {
                ushort[] pixels = GetGumpPixels(g, out int w, out int h);

                if (pixels == null || pixels.Length == 0)
                    return null;

                texture = new UOTexture16(w, h);
                texture.PushData(pixels);
                ResourceDictionary.Add(g, texture);
            }

            return texture;
        }

        public override void CleanResources()
        {
           
        }

        public unsafe ushort[] GetGumpPixels(uint index, out int width, out int height)
        {
            ref readonly var entry = ref GetValidRefEntry((int) index);

            if (entry.Extra == -1)
            {
                width = 0;
                height = 0;

                return null;
            }

            width = (entry.Extra >> 16) & 0xFFFF;
            height = entry.Extra & 0xFFFF;

            if (width == 0 || height == 0)
                return null;

            _file.SetData(entry.Address, entry.FileSize);

            _file.Seek(entry.Offset);

            IntPtr dataStart = _file.PositionAddress;

            ushort[] pixels = new ushort[width * height];
            int* lookuplist = (int*) dataStart;

            for (int y = 0; y < height; y++)
            {
                int gsize = 0;

                if (y < height - 1)
                    gsize = lookuplist[y + 1] - lookuplist[y];
                else
                    gsize = (entry.Length >> 2) - lookuplist[y];
                GumpBlock* gmul = (GumpBlock*) (dataStart + (lookuplist[y] << 2));
                int pos = y * width;

                for (int i = 0; i < gsize; i++)
                {
                    ushort val = gmul[i].Value;
                    ushort hue = (ushort) ((val != 0 ? 0x8000 : 0) | val);

                    int count = gmul[i].Run;

                    for (int j = 0; j < count; j++)
                        pixels[pos++] = hue == 0 && count == 1 ? (ushort)1 : hue;//avoid single zero pixels
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