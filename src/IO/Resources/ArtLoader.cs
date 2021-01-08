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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.IO.Resources
{
    internal class ArtLoader : UOFileLoader<ArtTexture>
    {
        private static ArtLoader _instance;
        private UOFile _file;
        private readonly ushort _graphicMask;
        private readonly UOTexture[] _landResources;
        private readonly LinkedList<uint> _usedLandTextureIds = new LinkedList<uint>();

        private ArtLoader(int staticCount, int landCount) : base(staticCount)
        {
            _graphicMask = Client.IsUOPInstallation ? (ushort) 0xFFFF : (ushort) 0x3FFF;
            _landResources = new UOTexture[landCount];
        }

        public static ArtLoader Instance => _instance ?? (_instance = new ArtLoader
            (Constants.MAX_STATIC_DATA_INDEX_COUNT, Constants.MAX_LAND_DATA_INDEX_COUNT));


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
        // ## BEGIN - END ## //
        public UOTexture GetLandTextureWF(uint g, bool isImpassable)
        {
            if (g >= _landResources.Length)
            {
                return null;
            }

            ref UOTexture texture = ref _landResources[g];

            if (texture == null || texture.IsDisposed)
            {
                ReadLandArtWF(ref texture, (ushort)g, isImpassable);

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
        // ## BEGIN - END ## //

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

        public unsafe uint[] ReadStaticArt(ushort graphic, out short width, out short height, out Rectangle bounds)
        {
            ref UOFileIndex entry = ref GetValidRefEntry(graphic + 0x4000);

            bounds = Rectangle.Empty;

            if (entry.Length == 0)
            {
                width = 0;
                height = 0;

                return null;
            }

            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);
            _file.Skip(4);
            width = _file.ReadShort();
            height = _file.ReadShort();

            if (width == 0 || height == 0)
            {
                return null;
            }

            uint[] pixels = new uint[width * height];
            ushort* ptr = (ushort*) _file.PositionAddress;
            ushort* lineoffsets = ptr;
            byte* datastart = (byte*) ptr + height * 2;
            int x = 0;
            int y = 0;
            ptr = (ushort*) (datastart + lineoffsets[0] * 2);
            int minX = width, minY = height, maxX = 0, maxY = 0;

            while (y < height)
            {
                ushort xoffs = *ptr++;
                ushort run = *ptr++;

                if (xoffs + run >= 2048)
                {
                    return null;
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
            // ## BEGIN - END ## //
            //else if (StaticFilters.IsCave
            //    (graphic) && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableCaveBorder)
            // ## BEGIN - END ## //
            else if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BlackOutlineStatics || StaticFilters.IsCave(graphic) && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableCaveBorder)
            // ## BEGIN - END ## //
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

            int pos1 = 0;

            for (y = 0; y < height; ++y)
            {
                for (x = 0; x < width; ++x)
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


            entry.Width = (short) ((width >> 1) - 22);
            entry.Height = (short) (height - 44);

            bounds.X = minX;
            bounds.Y = minY;
            bounds.Width = maxX - minX;
            bounds.Height = maxY - minY;

            return pixels;
        }


        private void ReadStaticArt(ref ArtTexture texture, ushort graphic)
        {
            uint[] pixels = ReadStaticArt(graphic, out short width, out short height, out Rectangle rect);

            if (pixels != null)
            {
                texture = new ArtTexture(width, height);
                texture.ImageRectangle = rect;
                texture.PushData(pixels);
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

            texture = new UOTexture(44, 44);
            // we don't need to store the data[] pointer because
            // land is always hoverable
            texture.SetDataPointerEXT(0, null, (IntPtr) data, SIZE * sizeof(uint));
        }

        // ## BEGIN - END ## //
        private unsafe void ReadLandArtWF(ref UOTexture texture, ushort graphic, bool isImpassable)
        {
            const int SIZE = 44 * 44;

            graphic &= _graphicMask;
            ref UOFileIndex entry = ref GetValidRefEntry(graphic);

            if (entry.Length == 0)
            {
                texture = null;

                return;
            }

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

                    if (j == end - 1 | j == end)
                    {
                        if (isImpassable)
                        {
                            data[pos++] = 0xFF_00_00_00;
                        }
                        else
                        {
                            data[pos++] = 0xAA_AA_AA_AA;
                        }
                    }
                }
            }

            for (int i = 0; i < 22; ++i)
            {
                int pos = (i + 22) * 44 + i;
                int end = i + ((22 - i) << 1);

                for (int j = i; j < end; ++j)
                {
                    data[pos++] = HuesHelper.Color16To32(_file.ReadUShort()) | 0xFF_00_00_00;

                    if (j == end - 1 | j == end - 2 | j == end - 3)
                    {
                        if (isImpassable)
                        {
                            data[pos++] = 0xFF_00_00_00;
                        }
                        else
                        {
                            data[pos++] = 0xAA_AA_AA_AA;
                        }
                    }
                }
            }

            texture = new UOTexture(44, 44);
            // we don't need to store the data[] pointer because
            // land is always hoverable
            texture.SetDataPointerEXT(0, null, (IntPtr)data, SIZE * sizeof(uint));
        }
        // ## BEGIN - END ## //
    }
}