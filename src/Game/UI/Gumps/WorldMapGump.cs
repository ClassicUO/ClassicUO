#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace ClassicUO.Game.UI.Gumps
{
    internal class WorldMapGump : ResizableGump
    {
        private UOTexture _mapTexture;
        private bool _isTopMost;
        private readonly float[] _zooms = new float[10] { 0.125f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 4f, 6f, 8f };
        private int _zoomIndex = 4;

        public WorldMapGump() : base(400, 400, 100, 100, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            //CanCloseWithRightClick = false;

            Load();
            OnResize();
        }


        public float Zoom => _zooms[_zoomIndex];


        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left)
                return base.OnMouseDoubleClick(x, y, button);

            _isTopMost = !_isTopMost;

            ShowBorder = !_isTopMost;

            ControlInfo.Layer = _isTopMost ? UILayer.Over : UILayer.Default;

            return true;
        }

        private unsafe void Load()
        {
            int size = FileManager.Map.MapsDefaultSize[World.MapIndex, 0] * FileManager.Map.MapsDefaultSize[World.MapIndex, 1];
            uint[] buffer = new uint[size];
            int maxBlock = size - 1;

            for (int bx = 0; bx < FileManager.Map.MapBlocksSize[World.MapIndex, 0]; bx++)
            {
                int mapX = bx << 3;

                for (int by = 0; by < FileManager.Map.MapBlocksSize[World.MapIndex, 1]; by++)
                {
                    IndexMap indexMap = World.Map.GetIndex(bx, by);

                    if (indexMap.MapAddress == 0)
                        continue;

                    int mapY = by << 3;
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
                            ref readonly StaticsBlock staticBlock = ref sb[c];

                            if (staticBlock.Color != 0 && staticBlock.Color != 0xFFFF && !GameObjectHelper.IsNoDrawable(staticBlock.Color))
                            {
                                pos = (staticBlock.Y << 3) + staticBlock.X;
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

                            ref readonly var c = ref infoCells[pos];
                            ushort color = (ushort)(0x8000 | FileManager.Hues.GetRadarColorData(c.TileID));

                            Color cc;

                            if (x > 0)
                            {
                                int index = (y * 8) + (x - 1);

                                if (c.Z < infoCells[index].Z)
                                {
                                    cc = new Color((((color >> 10) & 31) / 31f) * 80 / 100,
                                                   (((color >> 5) & 31) / 31f) * 80 / 100,
                                                   ((color & 31) / 31f) * 80 / 100);

                                }
                                else if (c.Z > infoCells[index].Z)
                                {
                                    cc = new Color((((color >> 10) & 31) / 31f) * 100 / 80,
                                                   (((color >> 5) & 31) / 31f) * 100 / 80,
                                                   ((color & 31) / 31f) * 100 / 80);
                                }
                                else
                                    cc = new Color((((color >> 10) & 31) / 31f),
                                                   (((color >> 5) & 31) / 31f),
                                                   ((color & 31) / 31f));
                            }
                            else
                            {
                                cc = new Color((((color >> 10) & 31) / 31f),
                                               (((color >> 5) & 31) / 31f),
                                               ((color & 31) / 31f));
                            }

                            buffer[block] = cc.PackedValue;

                            if (y < 7 && x < 7 && block < maxBlock)
                                buffer[block + 1] = cc.PackedValue;

                            block++;
                            pos++;
                        }
                    }
                }
            }

            _mapTexture = new UOTexture32(FileManager.Map.MapsDefaultSize[World.MapIndex, 0], FileManager.Map.MapsDefaultSize[World.MapIndex, 1]);
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
                    int realBlockX = i << 3;
                    int realBlockY = j << 3;

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

            _mapTexture = new UOTexture16(Width, Height);
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

                //if (data[block] == 0x8421)
                    data[block] = (ushort) color;
            }
        }
        
     
        public static (int, int) RotatePoint(int x, int y, float zoom, int dist, float angle = 45f)
        {
            x = (int)(x * zoom);
            y = (int)(y * zoom);

            if (angle == 0.0f)
                return (x, y);

            return ((int)Math.Round(Math.Cos(dist * Math.PI / 4.0) * x - Math.Sin(dist * Math.PI / 4.0) * y), (int)Math.Round(Math.Sin(dist * Math.PI / 4.0) * x + Math.Cos(dist * Math.PI / 4.0) * y));
        }


        protected override void OnMouseWheel(MouseEvent delta)
        {
            if (delta == MouseEvent.WheelScrollUp)
            {
                _zoomIndex++;

                if (_zoomIndex >= _zooms.Length)
                    _zoomIndex = _zooms.Length - 1;
            }
            else
            {
                _zoomIndex--;

                if (_zoomIndex < 0)
                    _zoomIndex = 0;
            }


            base.OnMouseWheel(delta);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            int gX = x + 4;
            int gY = y + 4;
            int gWidth = Width - 8;
            int gHeight = Height - 8;

            int sx = World.Player.X;
            int sy = World.Player.Y;

            int size = (int) Math.Max(gWidth * 1.75f, gHeight * 1.75f);
           

            int sw = (int) (size / Zoom);
            int sh = (int) (size / Zoom);

            int halfWidth = gWidth >> 1;
            int halfHeight = gHeight >> 1;

            ResetHueVector();


            if (_mapTexture == null)
                return false;

            var rect = ScissorStack.CalculateScissors(Matrix.Identity, gX, gY, gWidth, gHeight);

            if (ScissorStack.PushScissors(rect))
            {
                batcher.EnableScissorTest(true);

                int offset = size >> 1;


                batcher.Draw2D(_mapTexture, (x - offset) + halfWidth, (y - offset) + halfHeight,
                               size, size,

                               sx - (sw >> 1),
                               sy - (sh >> 1),

                               sw,
                               sh,

                               ref _hueVector, 45);

                batcher.EnableScissorTest(false);

                ScissorStack.PopScissors();
            }



            foreach (Mobile mobile in World.Mobiles)
            {
                if (mobile != World.Player)
                    DrawMobile(batcher, mobile, gX, gY, halfWidth, halfHeight, Zoom, Color.Red);
            }

            DrawMobile(batcher, World.Player, gX, gY, halfWidth, halfHeight, Zoom, Color.White);


            return base.Draw(batcher, x, y);
        }

        private void DrawMobile(UltimaBatcher2D batcher, Mobile mobile, int x, int y, int width, int height, float zoom, Color color)
        {
            int sx = mobile.X - World.Player.X;
            int sy = mobile.Y - World.Player.Y;

            (int rotX, int rotY) = RotatePoint(sx, sy, zoom, 1);

            rotX += x + width;
            rotY += y + height;

            batcher.Draw2D(Textures.GetTexture(color), rotX, rotY, 5, 5, ref _hueVector);
        }

        public override void Dispose()
        {
            _mapTexture?.Dispose();
            base.Dispose();
        }
    }
}