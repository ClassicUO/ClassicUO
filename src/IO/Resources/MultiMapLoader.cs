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

using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO.Resources
{
    internal class MultiMapLoader : UOFileLoader
    {
        private readonly UOFileMul[] _facets = new UOFileMul[6];
        private UOFile _file;

        private MultiMapLoader()
        {

        }

        private static MultiMapLoader _instance;
        public static MultiMapLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MultiMapLoader();
                }

                return _instance;
            }
        }



        internal bool HasFacet(int map)
        {
            return map >= 0 && map < _facets.Length && _facets[map] != null;
        }

        public override Task Load()
        {
            return Task.Run(() =>
            {
                string path = UOFileManager.GetUOFilePath("Multimap.rle");

                if (File.Exists(path))
                    _file = new UOFile(path, true);

                for (int i = 0; i < 6; i++)
                {
                    path = UOFileManager.GetUOFilePath($"facet0{i}.mul");

                    if (File.Exists(path)) _facets[i] = new UOFileMul(path);
                }
            });
        }

        public unsafe UOTexture LoadMap(int width, int height, int startx, int starty, int endx, int endy)
        {
            if (_file == null || _file.Length == 0)
            {
                Log.Warn( "MultiMap.rle is not loaded!");

                return null;
            }

            _file.Seek(0);

            int w = _file.ReadInt();
            int h = _file.ReadInt();

            if (w < 1 || h < 1)
            {
                Log.Warn( "Failed to load bounds from MultiMap.rle");

                return null;
            }

            int mapSize = width * height;

            startx = startx >> 1;
            endx = endx >> 1;

            int widthDivisor = endx - startx;

            if (widthDivisor == 0)
                widthDivisor++;

            starty = starty >> 1;
            endy = endy >> 1;

            int heightDivisor = endy - starty;

            if (heightDivisor == 0)
                heightDivisor++;

            int pwidth = (width << 8) / widthDivisor;
            int pheight = (height << 8) / heightDivisor;

            byte[] data = new byte[mapSize];

            int x = 0, y = 0;

            int maxPixelValue = 1;
            int startHeight = starty * pheight;

            while (_file.Position < _file.Length)
            {
                byte pic = _file.ReadByte();
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
                                maxPixelValue++;

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

            if (maxPixelValue >= 1)
            {
                int s = Marshal.SizeOf<HuesGroup>();
                IntPtr ptr = Marshal.AllocHGlobal(s * HuesLoader.Instance.HuesRange.Length);

                for (int i = 0; i < HuesLoader.Instance.HuesRange.Length; i++)
                    Marshal.StructureToPtr(HuesLoader.Instance.HuesRange[i], ptr + i * s, false);

                ushort* huesData = (ushort*) (byte*) (ptr + 30800);

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

                    worldMap[i] = (ushort) (bytepic != 0 ? colorTable[bytepic - 1] : 0);
                }

                Marshal.FreeHGlobal(ptr);

                UOTexture16 texture = new UOTexture16(width, height);
                texture.PushData(worldMap);

                return texture;
            }

            return null;
        }

        public UOTexture16 LoadFacet(int facet, int width, int height, int startx, int starty, int endx, int endy)
        {
            if (_file == null || facet < 0 || facet > 5 || _facets[facet] == null)
                return null;

            _facets[facet].Seek(0);

            int w = _facets[facet].ReadShort();
            int h = _facets[facet].ReadShort();

            if (w < 1 || h < 1) return null;

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
                        if (x >= startX && x < endX && y >= startY && y < endY)
                            map[(y - startY) * pwidth + (x - startX)] = color;
                        x++;
                    }
                }
            }

            UOTexture16 texture = new UOTexture16(pwidth, pheight);
            texture.PushData(map);

            return texture;
        }


        public override void CleanResources()
        {
            // do nothing
        }
    }
}