﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

namespace ClassicUO.IO.Resources
{
    class MultiMapLoader : ResourceLoader
    {
        private UOFile _file;
        private readonly UOFileMul[] _facets = new UOFileMul[6];

        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "Multimap.rle");

            if (File.Exists(path))
                _file = new UOFile(path);

            for (int i = 0; i < 6; i++)
            {
                path = Path.Combine(FileManager.UoFolderPath, $"facet0{i}.mul");

                if (File.Exists(path))
                {
                    _facets[i] = new UOFileMul(path, false);
                }
            }
        }

        public unsafe SpriteTexture LoadMap(int width, int height, int startx, int starty, int endx, int endy)
        {
            if (_file == null)
                return null;

            _file.Seek(0);

            int w = _file.ReadInt();
            int h = _file.ReadInt();

            if (w < 1 || h < 1)
            {
                return null;
            }


            int mapSize = width * height;
            byte[] data = new byte[mapSize];

            int startX = startx / 2;
            int endX = endx / 2;

            int widthDivisor = endX - startX;

            if (widthDivisor == 0)
                widthDivisor++;

            int startY = starty / 2;
            int endY = endy / 2;

            int heightDivisor = endY - startY;

            if (heightDivisor == 0)
                heightDivisor++;


            int pwidth = (width << 8) / widthDivisor;
            int pheight = (height << 8) / heightDivisor;

            int x = 0, y = 0;

            int maxPixelValue = 1;


            while (_file.Position < _file.Length)
            {
                byte pic = _file.ReadByte();
                byte size = (byte)(pic & 0x7F);

                bool colored = (pic & 0x80) != 0;

                int startHeight = startY * pheight;
                int currentHeight = y * pheight;
                int posY = width * ((currentHeight - startHeight) >> 8);

                for (int i = 0; i < size; i++)
                {
                    if (colored && x >= startX && x < endX && y >= startY && y < endY)
                    {
                        int position = posY + ((width * (x - startX)) >> 8);

                        ref byte pixel = ref data[position];

                        if (pixel < 0xFF)
                        {
                            if (pixel == maxPixelValue)
                                maxPixelValue++;

                            pixel++;
                        }
                    }

                    x++;

                    if (x >= w)
                    {
                        x = 0;
                        y++;
                        currentHeight += height;
                        posY = width * ((currentHeight - startHeight) >> 8);
                    }
                }

                if (maxPixelValue >= 1)
                {
                    int s = Marshal.SizeOf<HuesGroup>();
                    IntPtr ptr = Marshal.AllocHGlobal(s * FileManager.Hues.HuesRange.Length);
                    for (int i = 0; i < FileManager.Hues.HuesRange.Length; i++)
                        Marshal.StructureToPtr(FileManager.Hues.HuesRange[i], ptr + i * s, false);

                    ushort* huesData = (ushort*)(byte*)(ptr + 30800);

                    ushort[] colorTable = new ushort[maxPixelValue];

                    int colorOffset = 31 * maxPixelValue;

                    for (int i = 0; i < maxPixelValue; i++)
                    {
                        colorOffset -= 31;
                        colorTable[i] = (ushort) (0x8000 | huesData[colorOffset / maxPixelValue]);
                    }

                    ushort[] worldMap = new ushort[mapSize];

                    for (int i = 0; i < mapSize; i++)
                    {
                        byte bytepic = data[i];

                        worldMap[i] = (ushort) (pic != 0 ? colorTable[pic - 1] : 0);
                    }

                    Marshal.FreeHGlobal(ptr);

                    SpriteTexture texture = new SpriteTexture(width, height, false);
                    texture.SetData(worldMap);

                    return texture;
                }
            }

            return null;
        }

        public unsafe SpriteTexture LoadFacet(int facet, int width, int height, int startx, int starty, int endx, int endy)
        {
            if (_file == null || facet < 0 || facet > 5 || _facets[facet] == null)
                return null;

            _facets[facet].Seek(0);

            int w = _facets[facet].ReadShort();
            int h = _facets[facet].ReadShort();

            if (w < 1 || h < 1)
            {
                return null;
            }

            int startX = startx;
            int endX = endx;

            int startY = starty;
            int endY = endy;

            int pwidth = endX - startX;
            int pheight = endY - startY;

            ushort[] map = new ushort[pwidth * pheight];

            for (int y = 0; y < h; y++)
            {
                int x = 0;
                int colorCount = _facets[facet].ReadInt() / 3;

                for (int i = 0; i < colorCount; i++)
                {
                    int size = _facets[facet].ReadByte();

                    ushort color = (ushort) (0x8000 | _facets[facet].ReadUShort());

                    for (int j = 0; j < size; j++)
                    {
                        if ((x >= startX && x < endX) && (y >= startY && y < endY))
                            map[((y - startY) * pwidth) + (x - startX)] = color;
                        x++;
                    }
                }
            }

            SpriteTexture texture = new SpriteTexture(pwidth, pheight, false);
            texture.SetData(map);

            return texture;
        }


        protected override void CleanResources()
        {
            // do nothing
        }
    }
}
