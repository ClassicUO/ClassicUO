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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL2;

namespace ClassicUO.IO.Resources
{
    internal class ArtLoader : UOFileLoader<ArtTexture>
    {
        private static ArtLoader _instance;
        private UOFile _file;
        private readonly ushort _graphicMask;
        private readonly UOTexture[] _landResources;
        private readonly LinkedList<uint> _usedLandTextureIds = new LinkedList<uint>();
        private readonly PixelPicker _picker = new PixelPicker();

        private ArtLoader(int staticCount, int landCount) : base(staticCount)
        {
            _graphicMask = Client.IsUOPInstallation ? (ushort) 0xFFFF : (ushort) 0x3FFF;
            _landResources = new UOTexture[landCount];
        }

        public static ArtLoader Instance => _instance ?? (_instance = new ArtLoader(Constants.MAX_STATIC_DATA_INDEX_COUNT, Constants.MAX_LAND_DATA_INDEX_COUNT));


        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string filePath = UOFileManager.GetUOFilePath("artLegacyMUL.uop");

                    if (Client.IsUOPInstallation && File.Exists(filePath))
                    {
                        _file = new UOFileUop(filePath, "build/artlegacymul/{0:D8}.tga");
                        Entries = new UOFileIndex[Math.Max(((UOFileUop) _file).TotalEntriesCount, Constants.MAX_STATIC_DATA_INDEX_COUNT)];
                    }
                    else
                    {
                        filePath = UOFileManager.GetUOFilePath("art.mul");
                        string idxPath = UOFileManager.GetUOFilePath("artidx.mul");

                        if (File.Exists(filePath) && File.Exists(idxPath))
                        {
                            _file = new UOFileMul(filePath, idxPath, Constants.MAX_STATIC_DATA_INDEX_COUNT);
                        }
                    }

                    _file.FillEntries(ref Entries);
                }
            );
        }

        public override ArtTexture GetTexture(uint g)
        {
            if (g >= Resources.Length)
            {
                return null;
            }

            ref ArtTexture texture = ref Resources[g];

            if (texture == null || texture.IsDisposed)
            {
                ReadStaticArt(ref texture, (ushort) g);

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

        public UOTexture GetLandTexture(uint g)
        {
            if (g >= _landResources.Length)
            {
                return null;
            }

            ref UOTexture texture = ref _landResources[g];

            if (texture == null || texture.IsDisposed)
            {
                ReadLandArt(ref texture, (ushort) g);

                if (texture != null)
                {
                    _usedLandTextureIds.AddLast(g);
                }
            }
            else
            {
                texture.Ticks = Time.Ticks;
            }

            return texture;
        }

        public unsafe IntPtr CreateCursorSurfacePtr(int index, ushort customHue, out short w, out short h)
        {
            ref UOFileIndex entry = ref GetValidRefEntry(index + 0x4000);

            if (ReadHeader(_file, ref entry, out w, out h))
            {
                uint[] pixels = new uint[w * h];

                if (ReadData(pixels, w, h, _file))
                {
                    FinalizeData
                    (
                        pixels,
                        ref entry,
                        (ushort) index,
                        w,
                        h,
                        out _
                    );

                    fixed(uint * ptr = pixels)
                    {
                        SDL.SDL_Surface* surface = (SDL.SDL_Surface*)SDL.SDL_CreateRGBSurfaceWithFormatFrom
                        (
                            (IntPtr)ptr,
                            w,
                            h,
                            32,
                            4 * w,
                            SDL.SDL_PIXELFORMAT_ABGR8888
                        );

                        if (customHue > 0)
                        {
                            int stride = surface->pitch >> 2;
                            uint* pixels_ptr = (uint*)surface->pixels;
                            uint* p_line_end = pixels_ptr + w;
                            uint* p_img_end = pixels_ptr + stride * h;
                            int delta = stride - w;
                            Color c = default;

                            while (pixels_ptr < p_img_end)
                            {
                                while (pixels_ptr < p_line_end)
                                {
                                    if (*pixels_ptr != 0 && *pixels_ptr != 0xFF_00_00_00)
                                    {
                                        c.PackedValue = *pixels_ptr;
                                        *pixels_ptr = HuesHelper.Color16To32(HuesLoader.Instance.GetColor16(HuesHelper.ColorToHue(c), customHue)) | 0xFF_00_00_00;
                                    }

                                    ++pixels_ptr;
                                }

                                pixels_ptr += delta;
                                p_line_end += stride;
                            }
                        }

                        return (IntPtr)surface;
                    }
                }
            }
            
            return IntPtr.Zero;
        }

        public bool PixelCheck(int index, int x, int y)
        {
            return _picker.Get((ulong) index, x, y);
        }

        public override bool TryGetEntryInfo(int entry, out long address, out long size, out long compressedSize)
        {
            entry += 0x4000;

            if (entry < _file.Length && entry >= 0)
            {
                ref UOFileIndex e = ref GetValidRefEntry(entry);

                address = _file.StartAddress.ToInt64() + e.Offset;
                size = e.DecompressedLength == 0 ? e.Length : e.DecompressedLength;
                compressedSize = e.Length;

                return true;
            }

            return base.TryGetEntryInfo(entry, out address, out size, out compressedSize);
        }

        public override void ClearResources()
        {
            base.ClearResources();

            LinkedListNode<uint> first = _usedLandTextureIds.First;

            while (first != null)
            {
                LinkedListNode<uint> next = first.Next;
                uint idx = first.Value;

                if (idx < _landResources.Length)
                {
                    ref UOTexture texture = ref _landResources[idx];
                    texture?.Dispose();
                    texture = null;
                }

                _usedLandTextureIds.Remove(first);

                first = next;
            }
        }

        public override void CleaUnusedResources(int count)
        {
            base.CleaUnusedResources(count);
            ClearUnusedResources(_landResources, count);
        }



        private bool ReadHeader(DataReader file, ref UOFileIndex entry, out short width, out short height)
        {
            if (entry.Length == 0)
            {
                width = 0;
                height = 0;

                return false;
            }

            file.SetData(entry.Address, entry.FileSize);
            file.Seek(entry.Offset);
            file.Skip(4);
            width = file.ReadShort();
            height = file.ReadShort();

            return width > 0 && height > 0;
        }

        private unsafe bool ReadData(Span<uint> pixels, int width, int height, DataReader file)
        {
            ushort* ptr = (ushort*)file.PositionAddress;
            ushort* lineoffsets = ptr;
            byte* datastart = (byte*)ptr + height * 2;
            int x = 0;
            int y = 0;
            ptr = (ushort*)(datastart + lineoffsets[0] * 2);

            while (y < height)
            {
                ushort xoffs = *ptr++;
                ushort run = *ptr++;

                if (xoffs + run >= 2048)
                {
                    return false;
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
                            pixels[pos] = HuesHelper.Color16To32(val) | 0xFF_00_00_00;
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

            return true;
        }

        private void FinalizeData(Span<uint> pixels, ref UOFileIndex entry, ushort graphic, int width, int height, out Rectangle bounds)
        {
            int pos1 = 0;
            int minX = width, minY = height, maxX = 0, maxY = 0;

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
            else if (StaticFilters.IsCave(graphic) && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableCaveBorder)
            {
                AddBlackBorder(pixels, width, height);
            }

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (pixels[pos1++] != 0)
                    {
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            _picker.Set(graphic, width, height, pixels);

            entry.Width = (short)((width >> 1) - 22);
            entry.Height = (short)(height - 44);

            bounds.X = minX;
            bounds.Y = minY;
            bounds.Width = maxX - minX;
            bounds.Height = maxY - minY;
        }

        private unsafe void ReadStaticArt(ref ArtTexture texture, ushort graphic)
        {
            ref UOFileIndex entry = ref GetValidRefEntry(graphic + 0x4000);

            if (ReadHeader(_file, ref entry, out short width, out short height))
            {
                uint[] buffer = null;

                Span<uint> pixels = width * height <= 1024 ? stackalloc uint[1024] : (buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(width * height));

                try
                {
                    if (ReadData(pixels, width, height, _file))
                    {
                        texture = new ArtTexture(width, height);

                        FinalizeData
                        (
                            pixels,
                            ref entry,
                            graphic,
                            width,
                            height,
                            out texture.ImageRectangle
                        );


                        fixed (uint* ptr = pixels)
                        {
                            texture.SetDataPointerEXT(0, null, (IntPtr) ptr, width * height * sizeof(uint));
                        }
                    }
                }
                finally
                {
                    if (buffer != null)
                    {
                        System.Buffers.ArrayPool<uint>.Shared.Return(buffer, true);
                    }
                }
            }
        }

        private unsafe void ReadLandArt(ref UOTexture texture, ushort graphic)
        {
            const int SIZE = 44 * 44;

            graphic &= _graphicMask;
            ref UOFileIndex entry = ref GetValidRefEntry(graphic);

            if (entry.Length == 0)
            {
                texture = null;

                return;
            }

            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);

            uint* data = stackalloc uint[SIZE];

            for (int i = 0; i < 22; ++i)
            {
                int start = 22 - (i + 1);
                int pos = i * 44 + start;
                int end = start + ((i + 1) << 1);

                for (int j = start; j < end; ++j)
                {
                    data[pos++] = HuesHelper.Color16To32(_file.ReadUShort()) | 0xFF_00_00_00;
                }
            }

            for (int i = 0; i < 22; ++i)
            {
                int pos = (i + 22) * 44 + i;
                int end = i + ((22 - i) << 1);

                for (int j = i; j < end; ++j)
                {
                    data[pos++] = HuesHelper.Color16To32(_file.ReadUShort()) | 0xFF_00_00_00;
                }
            }


            //AddBlackBorder(data, 44, 44);

            texture = new UOTexture(44, 44);
            // we don't need to store the data[] pointer because
            // land is always hoverable
            texture.SetDataPointerEXT(0, null, (IntPtr) data, SIZE * sizeof(uint));
        }

        private void AddBlackBorder(Span<uint> pixels, int width, int height)
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
    }
}