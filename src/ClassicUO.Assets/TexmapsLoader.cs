#region license

// Copyright (c) 2024, andreakarasho
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

using ClassicUO.IO;
using ClassicUO.Utility;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class TexmapsLoader : UOFileLoader
    {
        private UOFile _file;

        public const int MAX_LAND_TEXTURES_DATA_INDEX_COUNT = 0x4000;

        public TexmapsLoader(UOFileManager fileManager) : base(fileManager) { }

        public UOFile File => _file;

        public override void Load()
        {
            string path = FileManager.GetUOFilePath("texmaps.mul");
            string pathidx = FileManager.GetUOFilePath("texidx.mul");

            FileSystemHelper.EnsureFileExists(path);
            FileSystemHelper.EnsureFileExists(pathidx);

            _file = new UOFileMul(path, pathidx);
            _file.FillEntries();
            string pathdef = FileManager.GetUOFilePath("TexTerr.def");

            if (System.IO.File.Exists(pathdef))
            {
                using (DefReader defReader = new DefReader(pathdef))
                {
                    while (defReader.Next())
                    {
                        int index = defReader.ReadInt();

                        if (index < 0 || index >= _file.Entries.Length)
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

                            if (checkindex < 0 || checkindex >= _file.Entries.Length)
                            {
                                continue;
                            }

                            _file.Entries[index] = _file.Entries[checkindex];
                        }
                    }
                }
            }
        }

        public TexmapInfo GetTexmap(uint idx)
        {
            ref UOFileIndex entry = ref _file.GetValidRefEntry((int)idx);

            if (entry.Length <= 0)
            {
                return default;
            }

            _file.Seek(entry.Offset, SeekOrigin.Begin);
            var size = entry.Length == 0x2000 ? 64 : 128;
            var data = new uint[size * size];

            for (int i = 0; i < size; ++i)
            {
                int pos = i * size;

                for (int j = 0; j < size; ++j)
                {
                    data[pos + j] = HuesHelper.Color16To32(_file.ReadUInt16()) | 0xFF_00_00_00;
                }
            }

            return new TexmapInfo()
            {
                Pixels = data,
                Width = size,
                Height = size
            };
        }
    }

    public ref struct TexmapInfo
    {
        public Span<uint> Pixels;
        public int Width;
        public int Height;
    }
}
