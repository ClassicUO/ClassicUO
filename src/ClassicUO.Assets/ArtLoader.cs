﻿#region license

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
    public sealed class ArtLoader : UOFileLoader
    {
        private UOFile _file;
        public const int MAX_LAND_DATA_INDEX_COUNT = 0x4000;
        public const int MAX_STATIC_DATA_INDEX_COUNT = 0x14000;

        public ArtLoader(UOFileManager fileManager) : base(fileManager)
        {
        }


        public UOFile File => _file;


        public override Task Load()
        {
            return Task.Run(() =>
            {
                string filePath = FileManager.GetUOFilePath("artLegacyMUL.uop");

                if (FileManager.IsUOPInstallation && System.IO.File.Exists(filePath))
                {
                    _file = new UOFileUop(filePath, "build/artlegacymul/{0:D8}.tga");
                }
                else
                {
                    filePath = FileManager.GetUOFilePath("art.mul");
                    string idxPath = FileManager.GetUOFilePath("artidx.mul");

                    if (System.IO.File.Exists(filePath) && System.IO.File.Exists(idxPath))
                    {
                        _file = new UOFileMul(filePath, idxPath);
                    }
                }

                _file.FillEntries();
            });
        }

        // public Rectangle GetRealArtBounds(int index) =>
        //     index + 0x4000 >= _spriteInfos.Length
        //         ? Rectangle.Empty
        //         : _spriteInfos[index + 0x4000].ArtBounds;

        private static uint[] LoadLand(UOFile file, ref readonly UOFileIndex entry, out short width, out short height)
        {
            if (entry.Length == 0)
            {
                width = 0;
                height = 0;

                return Array.Empty<uint>();
            }

            width = 44;
            height = 44;

            if (entry.File != null)
                file = entry.File;

            file.Seek(entry.Offset, SeekOrigin.Begin);

            var data = new uint[width * height];

            for (int i = 0; i < 22; ++i)
            {
                int start = 22 - (i + 1);
                int pos = i * 44 + start;
                int end = start + ((i + 1) << 1);

                for (int j = start; j < end; ++j)
                {
                    data[pos++] = HuesHelper.Color16To32(file.ReadUInt16()) | 0xFF_00_00_00;
                }
            }

            for (int i = 0; i < 22; ++i)
            {
                int pos = (i + 22) * 44 + i;
                int end = i + ((22 - i) << 1);

                for (int j = i; j < end; ++j)
                {
                    data[pos++] = HuesHelper.Color16To32(file.ReadUInt16()) | 0xFF_00_00_00;
                }
            }

            return data;
        }

        private static unsafe uint[] LoadArt(UOFile file, ref readonly UOFileIndex entry, out short width, out short height)
        {
            if (entry.Length == 0)
            {
                width = 0;
                height = 0;

                return Array.Empty<uint>();
            }

            if (entry.File != null)
                file = entry.File;

            file.Seek(entry.Offset, SeekOrigin.Begin);

            var flags = file.ReadUInt32();
            width = file.ReadInt16();
            height = file.ReadInt16();

            var buf = new byte[entry.Length];
            file.Read(buf);

            var data = new uint[width * height];

            fixed (byte* startPtr = buf)
            {
                ushort* lineoffsets = (ushort*)startPtr;
                byte* datastart = (byte*)startPtr + height * 2;
                int x = 0;
                int y = 0;
                var ptr = (ushort*)(datastart + lineoffsets[0] * 2);

                while (y < height)
                {
                    ushort xoffs = *ptr++;
                    ushort run = *ptr++;

                    if (xoffs + run >= 2048)
                    {
                        break;
                    }

                    if (xoffs + run != 0)
                    {
                        x += xoffs;
                        int pos = y * width + x;

                        for (int j = 0; j < run; ++j, ++pos)
                        {
                            ushort val = *ptr++;

                            if (val != 0)
                            {
                                data[pos] = HuesHelper.Color16To32(val) | 0xFF_00_00_00;
                            }
                        }

                        x += run;
                    }
                    else
                    {
                        x = 0;
                        ++y;
                        ptr = (ushort*)(datastart + lineoffsets[y] * 2);
                    }
                }
            }

            return data;
        }

        private static void AddBlackBorder(Span<uint> pixels, int width, int height)
        {
            for (int yy = 0; yy < height; yy++)
            {
                int startY = yy != 0 ? -1 : 0;
                int endY = yy + 1 < height ? 2 : 1;

                for (int xx = 0; xx < width; xx++)
                {
                    ref uint pixel = ref pixels[yy * width + xx];

                    if (pixel == 0)
                    {
                        continue;
                    }

                    int startX = xx != 0 ? -1 : 0;
                    int endX = xx + 1 < width ? 2 : 1;

                    for (int i = startY; i < endY; i++)
                    {
                        int currentY = yy + i;

                        for (int j = startX; j < endX; j++)
                        {
                            int currentX = xx + j;

                            ref uint currentPixel = ref pixels[currentY * width + currentX];

                            if (currentPixel == 0u)
                            {
                                pixel = 0xFF_00_00_00;
                            }
                        }
                    }
                }
            }
        }

        public ArtInfo GetArt(uint idx)
        {
            ref var entry = ref _file.GetValidRefEntry((int)idx);
            var loadLand = idx < 0x4000;
            var pixels = loadLand ? 
                LoadLand(_file, in entry, out var width, out var height)
                : 
                LoadArt(_file, in entry, out width, out height);

            return new ArtInfo()
            {
                Pixels = pixels,
                Width = width,
                Height = height
            };
        }
    }

    public ref struct ArtInfo
    {
        public Span<uint> Pixels;
        public int Width;
        public int Height;
    }
}
