#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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

                            if (index < 0 || index >= Entries.Length)
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

                                if (checkindex < 0 || checkindex >= Entries.Length)
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

            _file.SetData(entry.Address, entry.FileSize);
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