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
        private Point _center, _lastScroll;
        private bool _isScrolling;
        private bool _flipMap = true;
        private bool _freeView;

        public WorldMapGump() : base(400, 400, 100, 100, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;

            GameActions.Print("WorldMap loading...", 0x35);
            Task.Run(Load);
            OnResize();


            ContextMenuControl contextMenu = new ContextMenuControl();
            contextMenu.Add("Flip map", () => _flipMap = !_flipMap);
            contextMenu.Add("Top Most", () => TopMost = !TopMost);
            contextMenu.Add("Free view", () =>
            {
                _freeView = !_freeView;

                if (!_freeView)
                {
                    _isScrolling = false;
                    CanMove = true;
                }
            });
            contextMenu.Add("", null);
            contextMenu.Add("Close", Dispose);


            Add(contextMenu);
        }


        public float Zoom => _zooms[_zoomIndex];


        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button != MouseButton.Left || _isScrolling || Keyboard.Alt)
                return base.OnMouseDoubleClick(x, y, button);

            TopMost = !TopMost;
          
            return true;
        }

        public bool TopMost
        {
            get => _isTopMost;
            set
            {
                _isTopMost = value;

                ShowBorder = !_isTopMost;

                ControlInfo.Layer = _isTopMost ? UILayer.Over : UILayer.Default;

            }
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (!_freeView)
            {
                _isScrolling = false;
                CanMove = true;
            }

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if ((button == MouseButton.Left && Keyboard.Alt) || _freeView)
            {
                _lastScroll.X = x;
                _lastScroll.Y = y;
                _isScrolling = true;
                CanMove = false;
            }

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            Point offset = Mouse.LDroppedOffset;

            if (_isScrolling && offset != Point.Zero)
            {
                int scrollX = _lastScroll.X - x;
                int scrollY = _lastScroll.Y - y;

                (scrollX, scrollY) = RotatePoint(scrollX, scrollY, 1f, -1);

                _center.X += (int) (scrollX / Zoom);
                _center.Y += (int) (scrollY / Zoom);

                if (_center.X < 0)
                    _center.X = 0;

                if (_center.Y < 0)
                    _center.Y = 0;

                _lastScroll.X = x;
                _lastScroll.Y = y;
            }
            else
            {
                base.OnMouseOver(x, y);
            }
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
                    ref IndexMap indexMap = ref World.Map.GetIndex(bx, by);

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
                                int index = (y << 3) + (x - 1);

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

            GameActions.Print("WorldMap loaded!", 0x48);
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
            if (!_isScrolling && !_freeView)
            {
                _center.X = World.Player.X;
                _center.Y = World.Player.Y;
            }


            int gX = x + 4;
            int gY = y + 4;
            int gWidth = Width - 8;
            int gHeight = Height - 8;

            int sx = _center.X;
            int sy = _center.Y;

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

                               ref _hueVector, _flipMap ? 45 : 0);

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
            int sx = mobile.X - _center.X;
            int sy = mobile.Y - _center.Y;

            (int rotX, int rotY) = RotatePoint(sx, sy, zoom, 1);

            rotX += x + width;
            rotY += y + height;

            const int DOT_SIZE = 5;

            if (rotX < x)
                rotX = x;

            if (rotX > x + Width - 8 - DOT_SIZE)
                rotX = x + Width - 8 - DOT_SIZE;

            if (rotY < y)
                rotY = y;

            if (rotY > y + Height - 8 - DOT_SIZE)
                rotY = y + Height - 8 - DOT_SIZE;


            batcher.Draw2D(Textures.GetTexture(color), rotX, rotY, DOT_SIZE, DOT_SIZE, ref _hueVector);
        }

        public override void Dispose()
        {
            _mapTexture?.Dispose();
            base.Dispose();
        }
    }
}