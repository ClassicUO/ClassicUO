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

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class MultiMapLoader : UOFileLoader
    {
        private static MultiMapLoader _instance;
        private UOFileMul[] _facets;
        private UOFile _file;

        private MultiMapLoader()
        {
        }

        public static MultiMapLoader Instance => _instance ?? (_instance = new MultiMapLoader());

        public bool HasFacet(int map)
        {
            return map >= 0 && map < _facets.Length && _facets[map] != null;
        }

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("Multimap.rle");

                    if (File.Exists(path))
                    {
                        _file = new UOFile(path, true);
                    }

                    var facetFiles = Directory.GetFiles(UOFileManager.BasePath, "*.mul", SearchOption.TopDirectoryOnly)
                        .Select(s => Regex.Match(s, "facet0.*\\.mul", RegexOptions.IgnoreCase))
                        .Where(s => s.Success)
                        .Select(s => Path.Combine(UOFileManager.BasePath, s.Value))
                        .OrderBy(s => s)
                        .ToArray();

                    _facets = new UOFileMul[facetFiles.Length];

                    for (int i = 0; i < facetFiles.Length; i++)
                    {
                        _facets[i] = new UOFileMul(facetFiles[i]);
                    }
                }
            );
        }

        public unsafe MultiMapInfo LoadMap
        (
            int width,
            int height,
            int startx,
            int starty,
            int endx,
            int endy
        )
        {
            if (_file == null || _file.Length == 0)
            {
                Log.Warn("MultiMap.rle is not loaded!");

                return default;
            }

            _file.Seek(0);

            int w = _file.ReadInt();
            int h = _file.ReadInt();

            if (w < 1 || h < 1)
            {
                Log.Warn("Failed to load bounds from MultiMap.rle");

                return default;
            }

            int mapSize = width * height;

            startx = startx >> 1;
            endx = endx >> 1;

            int widthDivisor = endx - startx;

            if (widthDivisor == 0)
            {
                widthDivisor++;
            }

            starty = starty >> 1;
            endy = endy >> 1;

            int heightDivisor = endy - starty;

            if (heightDivisor == 0)
            {
                heightDivisor++;
            }

            int pwidth = (width << 8) / widthDivisor;
            int pheight = (height << 8) / heightDivisor;

            byte[] data = new byte[mapSize];

            int x = 0, y = 0;

            int maxPixelValue = 1;
            int startHeight = starty * pheight;

            while (_file.Position < _file.Length)
            {
                byte pic = _file.ReadByte();
                byte size = (byte)(pic & 0x7F);
                bool colored = (pic & 0x80) != 0;

                int currentHeight = y * pheight;
                int posY = width * ((currentHeight - startHeight) >> 8);

                for (int i = 0; i < size; i++)
                {
                    if (colored && x >= startx && x < endx && y >= starty && y < endy)
                    {
                        int position = posY + ((pwidth * (x - startx)) >> 8);

                        ref byte pixel = ref data[position];

                        if (pixel < 0xFF)
                        {
                            if (pixel == maxPixelValue)
                            {
                                maxPixelValue++;
                            }

                            pixel++;
                        }
                    }

                    x++;

                    if (x >= w)
                    {
                        x = 0;
                        y++;
                        currentHeight += pheight;
                        posY = width * ((currentHeight - startHeight) >> 8);
                    }
                }
            }

            if (maxPixelValue <= 0)
            {
                return default;
            }

            int s = Marshal.SizeOf<HuesGroup>();
            IntPtr ptr = Marshal.AllocHGlobal(s * HuesLoader.Instance.HuesRange.Length);

            for (int i = 0; i < HuesLoader.Instance.HuesRange.Length; i++)
            {
                Marshal.StructureToPtr(HuesLoader.Instance.HuesRange[i], ptr + i * s, false);
            }

            ushort* huesData = (ushort*)(byte*)(ptr + 30800);

            Span<uint> colorTable = stackalloc uint[byte.MaxValue];
            var pixels = new uint[mapSize];

            try
            {
                int colorOffset = 31 * maxPixelValue;

                for (int i = 0; i < maxPixelValue; i++)
                {
                    colorOffset -= 31;
                    colorTable[i] = HuesHelper.Color16To32(huesData[colorOffset / maxPixelValue]) | 0xFF_00_00_00;
                }

                for (int i = 0; i < mapSize; i++)
                {
                    pixels[i] = data[i] != 0 ? colorTable[data[i] - 1] : 0;
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }

            return new MultiMapInfo()
            {
                Pixels = pixels,
                Width = width,
                Height = height,
            };
        }

        public MultiMapInfo LoadFacet
        (
            int facet,
            int width,
            int height,
            int startx,
            int starty,
            int endx,
            int endy
        )
        {
            if (_file == null || facet < 0 || facet > MapLoader.MAPS_COUNT || facet >= _facets.Length || _facets[facet] == null)
            {
                return default;
            }

            _facets[facet].Seek(0);

            int w = _facets[facet].ReadShort();

            int h = _facets[facet].ReadShort();

            if (w < 1 || h < 1)
            {
                return default;
            }

            int startX = startx;
            int endX = endx <= 0 ? width : endx;

            int startY = starty;
            int endY = endy <= 0 ? height : endy;

            int pwidth = endX - startX;
            int pheight = endY - startY;

            var pixels = new uint[pwidth * pheight];

            for (int y = 0; y < h; y++)
            {
                int x = 0;

                int colorCount = _facets[facet].ReadInt() / 3;

                for (int i = 0; i < colorCount; i++)
                {
                    int size = _facets[facet].ReadByte();

                    uint color = HuesHelper.Color16To32(_facets[facet].ReadUShort()) | 0xFF_00_00_00;

                    for (int j = 0; j < size; j++)
                    {
                        if (x >= startX && x < endX && y >= startY && y < endY)
                        {
                            pixels[(y - startY) * pwidth + (x - startX)] = color;
                        }

                        x++;
                    }
                }
            }

            return new MultiMapInfo()
            {
                Pixels = pixels,
                Width = pwidth,
                Height = pheight,
            };
        }
    }

    public ref struct MultiMapInfo
    {
        public Span<uint> Pixels;
        public int Width, Height;
    }
}
