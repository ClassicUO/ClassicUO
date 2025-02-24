// SPDX-License-Identifier: BSD-2-Clause

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
    public sealed class MultiMapLoader : UOFileLoader
    {
        private UOFileMul[] _facets;
        private UOFile _file;

        public MultiMapLoader(UOFileManager fileManager) : base(fileManager)
        {
        }

        public bool HasFacet(int map)
        {
            return map >= 0 && map < _facets.Length && _facets[map] != null;
        }

        public override void Load()
        {
            string path = FileManager.GetUOFilePath("Multimap.rle");

            if (File.Exists(path))
            {
                _file = new UOFile(path);
            }

            _facets = Directory.GetFiles(FileManager.BasePath, "*.mul", SearchOption.TopDirectoryOnly)
                .Select(s => Regex.Match(s, "facet0.*\\.mul", RegexOptions.IgnoreCase))
                .Where(s => s.Success)
                .Select(s => Path.Combine(FileManager.BasePath, s.Value))
                .OrderBy(s => s)
                .Select(s => new UOFileMul(s))
                .ToArray();
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

            _file.Seek(0, SeekOrigin.Begin);

            int w = _file.ReadInt32();
            int h = _file.ReadInt32();

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
                byte pic = _file.ReadUInt8();
                byte size = (byte) (pic & 0x7F);
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
            IntPtr ptr = Marshal.AllocHGlobal(s * FileManager.Hues.HuesRange.Length);

            for (int i = 0; i < FileManager.Hues.HuesRange.Length; i++)
            {
                Marshal.StructureToPtr(FileManager.Hues.HuesRange[i], ptr + i * s, false);
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

            var file = _facets[facet];
            file.Seek(0, SeekOrigin.Begin);

            int w = file.ReadUInt16();
            int h = file.ReadUInt16();

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

                int colorCount = file.ReadInt32() / 3;

                for (int i = 0; i < colorCount; i++)
                {
                    int size = file.ReadUInt8();

                    uint color = HuesHelper.Color16To32(file.ReadUInt16()) | 0xFF_00_00_00;

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
