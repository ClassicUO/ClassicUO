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
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class GumpsLoader : UOFileLoader
    {
        public const int MAX_GUMP_DATA_INDEX_COUNT = 0x10000;


        private UOFile _file;

        public GumpsLoader(UOFileManager fileManager) : base(fileManager) { }


        public bool UseUOPGumps = false;


        public override Task Load()
        {
            return Task.Run(() =>
            {
                string path = FileManager.GetUOFilePath("gumpartLegacyMUL.uop");

                if (FileManager.IsUOPInstallation && File.Exists(path))
                {
                    _file = new UOFileUop(path, "build/gumpartlegacymul/{0:D8}.tga", true);
                    Entries = new UOFileIndex[
                        Math.Max(((UOFileUop)_file).TotalEntriesCount, MAX_GUMP_DATA_INDEX_COUNT)
                    ];
                    UseUOPGumps = true;
                }
                else
                {
                    path = FileManager.GetUOFilePath("gumpart.mul");
                    string pathidx = FileManager.GetUOFilePath("gumpidx.mul");

                    if (!File.Exists(path))
                    {
                        path = FileManager.GetUOFilePath("Gumpart.mul");
                    }

                    if (!File.Exists(pathidx))
                    {
                        pathidx = FileManager.GetUOFilePath("Gumpidx.mul");
                    }

                    _file = new UOFileMul(path, pathidx);

                    UseUOPGumps = false;
                }

                _file.FillEntries(ref Entries);

                string pathdef = FileManager.GetUOFilePath("gump.def");

                if (!File.Exists(pathdef))
                {
                    return;
                }

                using (DefReader defReader = new DefReader(pathdef, 3))
                {
                    while (defReader.Next())
                    {
                        int ingump = defReader.ReadInt();

                        if (
                            ingump < 0
                            || ingump >= MAX_GUMP_DATA_INDEX_COUNT
                            || ingump >= Entries.Length
                            || Entries[ingump].Length > 0
                        )
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
                            int checkIndex = group[i];

                            if (
                                checkIndex < 0
                                || checkIndex >= MAX_GUMP_DATA_INDEX_COUNT
                                || checkIndex >= Entries.Length
                                || Entries[checkIndex].Length <= 0
                            )
                            {
                                continue;
                            }

                            Entries[ingump] = Entries[checkIndex];

                            Entries[ingump].Hue = (ushort)defReader.ReadInt();

                            break;
                        }
                    }
                }
            });
        }

        public GumpInfo GetGump(uint index)
        {
            ref var entry = ref GetValidRefEntry((int)index);

            if (entry.CompressionFlag != CompressionType.ZlibBwt && entry.Width <= 0 && entry.Height <= 0)
            {
                return default;
            }

            ushort color = entry.Hue;

            var reader = new StackDataReader((IntPtr)(entry.Address + entry.Offset), entry.Length);
            var w = (uint)entry.Width;
            var h = (uint)entry.Height;

            if (entry.CompressionFlag >= CompressionType.Zlib)
            {
                var dbuf = new byte[entry.DecompressedLength];

                unsafe
                {
                    fixed (byte* dstPtr = dbuf)
                    {
                        var result = ZLib.Decompress(reader.PositionAddress, entry.Length, 0, (IntPtr)dstPtr, dbuf.Length);
                        if (result != ZLib.ZLibError.Okay)
                        {
                            return default;
                        }
                    }
                }

                var output = entry.CompressionFlag == CompressionType.ZlibBwt ? BwtDecompress.Decompress(dbuf) : dbuf;
                reader = new StackDataReader(output);
                w = reader.ReadUInt32LE();
                h = reader.ReadUInt32LE();
            }

            Span<uint> pixels = new uint[w * h];
            var len = reader.Remaining;
            var halfLen = len >> 2;

            var start = reader.Position;
            var rowLookup = new int[h];
            for (var y = 0; y < h; ++y)
                rowLookup[y] = reader.ReadInt32LE();

            for (var y = 0; y < h; ++y)
            {
                var gsize = (y < h - 1) ? rowLookup[y + 1] - rowLookup[y] : halfLen - rowLookup[y];
                reader.Seek(start + (rowLookup[y] << 2));

                var pixelIndex = (int)(y * w);
                for (var i = 0; i < gsize; ++i)
                {
                    var value = reader.ReadUInt16LE();
                    var run = reader.ReadUInt16LE();
                    var rbga = 0u;

                    if (color != 0 && value != 0)
                    {
                        value = FileManager.Hues.GetColor16(value, color);
                    }

                    if (value != 0)
                    {
                        rbga = HuesHelper.Color16To32(value) | 0xFF_00_00_00;
                    }

                    pixels.Slice(pixelIndex, run).Fill(rbga);
                    pixelIndex += run;
                }
            }

            return new GumpInfo()
            {
                Pixels = pixels,
                Width = (int)w,
                Height = (int)h
            };
        }
    }

    public ref struct GumpInfo
    {
        public Span<uint> Pixels;
        public int Width;
        public int Height;
    }
}
