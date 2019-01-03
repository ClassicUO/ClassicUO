#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using ClassicUO.Game.Map;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MapGump : Gump
    {
        private SpriteTexture _mapTexture;

        public MapGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            Width = 400;
            Height = 400;

            //using (FileStream stream = File.OpenRead(@"D:\Progetti\UO\map\Maps\2Dmap0.png"))
            //    _mapTexture = Texture2D.FromStream(Service.GetByLocalSerial<SpriteBatch3D>().GraphicsDevice, stream);
        }

        private unsafe void Load()
        {
            int size = FileManager.Map.MapsDefaultSize[World.MapIndex, 0] * FileManager.Map.MapsDefaultSize[World.MapIndex, 1];
            ushort[] buffer = new ushort[size];
            int maxBlock = size - 1;

            for (int bx = 0; bx < FileManager.Map.MapBlocksSize[World.MapIndex, 0]; bx++)
            {
                int mapX = bx * 8;

                for (int by = 0; by < FileManager.Map.MapBlocksSize[World.MapIndex, 1]; by++)
                {
                    IndexMap indexMap = World.Map.GetIndex(bx, by);

                    if (indexMap.MapAddress == 0)
                        continue;
                    int mapY = by * 8;
                    MapBlock info = new MapBlock();
                    MapCells* infoCells = (MapCells*) &info.Cells;
                    MapBlock* mapBlock = (MapBlock*) indexMap.MapAddress;
                    MapCells* cells = (MapCells*) &mapBlock->Cells;
                    int pos = 0;

                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            ref MapCells cell = ref cells[pos];
                            ref MapCells infoCell = ref infoCells[pos];
                            infoCell.TileID = cell.TileID;
                            infoCell.Z = cell.Z;
                            pos++;
                        }
                    }

                    StaticsBlock* sb = (StaticsBlock*) indexMap.StaticAddress;

                    if (sb != null)
                    {
                        int count = (int) indexMap.StaticCount;

                        for (int c = 0; c < count; c++)
                        {
                            StaticsBlock staticBlock = sb[c];

                            if (staticBlock.Color > 0 && staticBlock.Color != 0xFFFF && !GameObjectHelper.IsNoDrawable(staticBlock.Color))
                            {
                                pos = staticBlock.Y * 8 + staticBlock.X;
                                ref MapCells cell = ref infoCells[pos];

                                if (cell.Z <= staticBlock.Z)
                                {
                                    cell.TileID = (ushort) (staticBlock.Color + 0x4000);
                                    cell.Z = staticBlock.Z;
                                }
                            }
                        }
                    }

                    pos = 0;

                    for (int y = 0; y < 8; y++)
                    {
                        int block = (mapY + y) * FileManager.Map.MapsDefaultSize[World.MapIndex, 0] + mapX;

                        for (int x = 0; x < 8; x++)
                        {
                            ushort color = (ushort) (0x8000 | FileManager.Hues.GetRadarColorData(infoCells[pos].TileID));
                            buffer[block] = color;

                            if (y < 7 && x < 7 && block < maxBlock)
                                buffer[block + 1] = color;
                            block++;
                            pos++;
                        }
                    }
                }
            }

            _mapTexture = new SpriteTexture(FileManager.Map.MapsDefaultSize[World.MapIndex, 0], FileManager.Map.MapsDefaultSize[World.MapIndex, 1], false);
            _mapTexture.SetData(buffer);
        }

        public Texture2D Load2()
        {
            int lastX = World.Player.Position.X;
            int lastY = World.Player.Position.Y;
            int blockOffsetX = Width >> 2;
            int blockOffsetY = Height >> 2;
            int gumpCenterX = Width >> 1;
            int gumpCenterY = Height >> 1;

            //0xFF080808 - pixel32
            //0x8421 - pixel16
            int minBlockX = ((lastX - blockOffsetX) >> 3) - 1;
            int minBlockY = ((lastY - blockOffsetY) >> 3) - 1;
            int maxBlockX = ((lastX + blockOffsetX) >> 3) + 1;
            int maxBlockY = ((lastY + blockOffsetY) >> 3) + 1;

            if (minBlockX < 0)
                minBlockX = 0;

            if (minBlockY < 0)
                minBlockY = 0;
            int maxBlockIndex = World.Map.MapBlockIndex;
            int mapBlockHeight = FileManager.Map.MapBlocksSize[World.MapIndex, 1];
            ushort[] data = new ushort[Width * Height];

            for (int i = minBlockX; i <= maxBlockX; i++)
            {
                int blockIndexOffset = i * mapBlockHeight;

                for (int j = minBlockY; j <= maxBlockY; j++)
                {
                    int blockIndex = blockIndexOffset + j;

                    if (blockIndex >= maxBlockIndex)
                        break;
                    RadarMapBlock? mbbv = FileManager.Map.GetRadarMapBlock(World.MapIndex, i, j);

                    if (!mbbv.HasValue)
                        break;
                    RadarMapBlock mb = mbbv.Value;
                    Chunk block = World.Map.Chunks[blockIndex];
                    int realBlockX = i * 8;
                    int realBlockY = j * 8;

                    for (int x = 0; x < 8; x++)
                    {
                        int px = realBlockX + x - lastX + gumpCenterX;

                        for (int y = 0; y < 8; y++)
                        {
                            int py = realBlockY + y - lastY;
                            int gx = px - py;
                            int gy = px + py;
                            uint color = mb.Cells[x, y].Graphic;
                            bool island = mb.Cells[x, y].IsLand;

                            //if (block != null)
                            //{
                            //    ushort multicolor = block.get
                            //}

                            if (!island)
                                color += 0x4000;
                            int tableSize = 2;
                            color = (uint) (0x8000 | FileManager.Hues.GetRadarColorData((int) color));

                            Point[] table = new Point[2]
                            {
                                new Point(0, 0), new Point(0, 1)
                            };
                            CreatePixels(data, (int) color, gx, gy, Width, Height, table, tableSize);
                        }
                    }
                }
            }

            _mapTexture = new SpriteTexture(Width, Height, false);
            _mapTexture.SetData(data);

            return _mapTexture;
        }

        private void CreatePixels(ushort[] data, int color, int x, int y, int w, int h, Point[] table, int count)
        {
            int px = x;
            int py = y;

            for (int i = 0; i < count; i++)
            {
                px += table[i].X;
                py += table[i].Y;
                int gx = px;

                if (gx < 0 || gx >= w)
                    continue;
                int gy = py;

                if (gy < 0 || gy >= h)
                    break;
                int block = gy * w + gx;

                if (data[block] == 0x8421)
                    data[block] = (ushort) color;
            }
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            //batcher.Draw2D(_mapTexture, Bounds, position);

            //batcher.Draw2D(_mapTexture, new Rectangle((int)position.X, (int)position.Y, Width, Height), _mapTexture.Bounds, Vector3.Zero);

            //Draw(batcher, new Rectangle((int)position.X, (int)position.Y, Width, Height), 0, 0);

            return base.Draw(batcher, position, hue);
        }

        public static Vector2 RotateVector2(Vector2 point, float radians, Vector2 pivot)
        {
            float cosRadians = (float) Math.Cos(radians);
            float sinRadians = (float) Math.Sin(radians);
            Vector2 translatedPoint = new Vector2();
            translatedPoint.X = point.X - pivot.X;
            translatedPoint.Y = point.Y - pivot.Y;
            Vector2 rotatedPoint = new Vector2();
            rotatedPoint.X = translatedPoint.X * cosRadians - translatedPoint.Y * sinRadians + pivot.X;
            rotatedPoint.Y = translatedPoint.X * sinRadians + translatedPoint.Y * cosRadians + pivot.Y;

            return rotatedPoint;
        }

        public bool Draw(Batcher2D batcher, Rectangle dst, int offsetX, int offsetY)
        {
            Rectangle src = new Rectangle();

            //if (offsetX > Width || offsetX < -MaxWidth || offsetY > Height || offsetY < -Height)
            //    return false;
            src.X = offsetX;
            src.Y = offsetY;
            int maxX = src.X + dst.Width;

            if (maxX <= Width)
                src.Width = dst.Width;
            else
            {
                src.Width = Width - src.X;
                dst.Width = src.Width;
            }

            int maxY = src.Y + dst.Height;

            if (maxY <= Height)
                src.Height = dst.Height;
            else
            {
                src.Height = Height - src.Y;
                dst.Height = src.Height;
            }

            //src.X = World.Player.Position.X - src.Width / 2;
            //src.Y = World.Player.Position.Y - src.Height / 2;

            //var rotDest = RotateVector2(new Vector2(dst.X, dst.Y), 45, new Vector2(dst.Width / 2, dst.Height / 2));

            //dst.X = (int)rotDest.X;
            //dst.Y = (int)rotDest.Y;

            return batcher.Draw2D(_mapTexture, dst, src, Vector3.Zero);
        }
    }
}