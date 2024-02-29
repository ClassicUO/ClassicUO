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

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MapGump : Gump
    {
        private readonly Button[] _buttons = new Button[3];
        private readonly List<Control> _container = new List<Control>();
        private PinControl _currentPin;
        private Point _lastPoint;
        private HitBox _hit;
        private Texture2D _mapTexture;
        private ResizePic mapGump;

        private uint _pinTimer;

        public MapGump(uint serial, ushort gumpid, int width, int height) : base(serial, 0)
        {
            AcceptMouseInput = false;
            CanMove = true;
            CanCloseWithRightClick = true;
            Width = width;
            Height = height;

            WantUpdateSize = false;

            Add
            (mapGump =
                new ResizePic(0x1432)
                {
                    Width = width + 44,
                    Height = height + 61
                }
            );

            Add(_buttons[0] = new Button((int)ButtonType.PlotCourse, 0x1398, 0x1398) { X = (width - 100) >> 1, Y = 5, ButtonAction = ButtonAction.Activate });

            Add(_buttons[1] = new Button((int)ButtonType.StopPlotting, 0x1399, 0x1399) { X = (width - 70) >> 1, Y = 5, ButtonAction = ButtonAction.Activate });

            Add(_buttons[2] = new Button((int)ButtonType.ClearCourse, 0x139A, 0x139A) { X = (width - 66) >> 1, Y = height + 37, ButtonAction = ButtonAction.Activate });

            _buttons[0].IsVisible = _buttons[0].IsEnabled = PlotState == 0;

            _buttons[1].IsVisible = _buttons[1].IsEnabled = PlotState == 1;

            _buttons[2].IsVisible = _buttons[2].IsEnabled = PlotState == 1;



            _hit = new HitBox(24, 31, width, height, null, 0f) { CanMove = true };
            Add(_hit);

            _hit.MouseUp += TextureControlOnMouseUp;

            MenuButton menu = new MenuButton(25, Color.Black.PackedValue, 0.75f, "Menu") { X = width + 44 - 43, Y = 6 };

            menu.MouseUp += (s, e) =>
            {
                menu.ContextMenu?.Show();
            };
            menu.ContextMenu = new ContextMenuControl();
            menu.ContextMenu.Add(new ContextMenuItemEntry("Show approximate location on world map", () =>
            {
                if (foundMapLoc)
                {
                    WorldMapGump map = UIManager.GetGump<WorldMapGump>();
                    if (map != null)
                    {
                        if (mapFacet != -1)
                        {
                            if (World.MapIndex != mapFacet)
                                GameActions.Print("You're on the wrong facet!", 32);
                            else
                                map.GoToMarker(mapX, mapY, true);
                        }
                        else
                            map.GoToMarker(mapX, mapY, true);
                    }
                }
            }));
            menu.ContextMenu.Add(new ContextMenuItemEntry("Add as marker on world map", () =>
            {
                if (foundMapLoc)
                {
                    WorldMapGump map = UIManager.GetGump<WorldMapGump>();
                    if (map != null)
                    {
                        if (mapFacet != -1)
                        {
                            map.AddUserMarker("TMap", mapX, mapY, mapFacet);
                        }
                        else
                            map.AddUserMarker("TMap", mapX, mapY, World.Map.Index);
                    }
                }
            }));
            menu.ContextMenu.Add(new ContextMenuItemEntry("Try to pathfind", () =>
            {
                if (foundMapLoc)
                {
                    int distance = Math.Max(Math.Abs(World.Player.X - mapX), Math.Abs(World.Player.Y - mapY));

                    if (distance > 10)
                    {
                        GameActions.Print("You're too far away to try to pathfind, you need to be within 10 tiles.", 32);
                        return;
                    }

                    if (mapFacet != -1)
                    {
                        if (World.MapIndex != mapFacet)
                            GameActions.Print("You're on the wrong facet!", 32);
                        else
                            Pathfinder.WalkTo(mapX, mapY, 0, 1);
                    }
                    else
                        Pathfinder.WalkTo(mapX, mapY, 0, 1);
                }
            }));
            menu.ContextMenu.Add(new ContextMenuItemEntry("Close", () => { Dispose(); }));
            menu.CanCloseWithRightClick = false;

            Add(new GumpPic(width - 20, height - 20, 0x0139D, 0));
            Add(menu);
        }


        public int PlotState { get; private set; }

        public void SetMapTexture(Texture2D texture)
        {
            _mapTexture?.Dispose();
            _mapTexture = texture;

            Width = texture.Width;
            Height = texture.Height;

            WantUpdateSize = true;
        }

        private int mapX = 0, mapY = 0, mapFacet = -1, mapEndX = 0, mapEndY = 0;
        private bool foundMapLoc = false;

        public void MapInfos(int x, int y, int endX, int endY, int facet = -1)
        {
            mapX = x;
            mapY = y;
            mapEndX = endX;
            mapEndY = endY;
            mapFacet = facet;
        }

        public void AddPin(int x, int y)
        {
            PinControl c = new PinControl(x, y);
            c.X += c.Width + 5;
            c.Y += c.Height;
            c.NumberText = (_container.Count + 1).ToString();
            _container.Add(c);
            Add(c);
            if (!foundMapLoc)
            {
                //multiplier = float((mapinfo.MapEnd.X) - (mapinfo.MapOrigin.X)) / float(width)
                //    multiX = mapinfo.PinPosition.X * multiplier
                //    multiY = mapinfo.PinPosition.Y * multiplier
                //    finalX = int(mapinfo.MapOrigin.X + multiX)
                //    finalY = int(mapinfo.MapOrigin.Y + multiY)

                float multiplier = (float)Width / 300f;
                //if (Width == 200)
                //    multiplier = 0.666666666f;
                //if (Width == 600)
                //    multiplier = 2f;
                if (CUOEnviroment.Debug)
                    GameActions.Print($"Width: {Width}, Multiplier: {multiplier}, Facet: {mapFacet}, MapData: {mapX}, {mapY}, {mapEndX}, {mapEndY}");

                mapX = (int)(mapX + (x * multiplier));
                mapY = (int)(mapY + (y * multiplier));

                //mapX = mapX + x;
                //mapY = mapY + y;
                foundMapLoc = true;

                _hit?.SetTooltip($"Estimated loc: {mapX}, {mapY}");
            }
        }

        public void ClearContainer()
        {
            foreach (Control s in _container)
            {
                s.Dispose();
            }

            _container.Clear();
        }

        public void SetPlotState(int s)
        {
            PlotState = s;

            _buttons[0].IsVisible = _buttons[0].IsEnabled = PlotState == 0;

            _buttons[1].IsVisible = _buttons[1].IsEnabled = PlotState == 1;

            _buttons[2].IsVisible = _buttons[2].IsEnabled = PlotState == 1;
        }

        public override void OnButtonClick(int buttonID)
        {
            ButtonType b = (ButtonType)buttonID;

            switch (b)
            {
                case ButtonType.PlotCourse:
                case ButtonType.StopPlotting:
                    NetClient.Socket.Send_MapMessage(LocalSerial,
                                                     6,
                                                     (byte)PlotState,
                                                     unchecked((ushort)-24),
                                                     unchecked((ushort)-31));

                    SetPlotState(PlotState == 0 ? 1 : 0);

                    break;

                case ButtonType.ClearCourse:
                    NetClient.Socket.Send_MapMessage(LocalSerial,
                                                     5,
                                                     0,
                                                     unchecked((ushort)-24),
                                                     unchecked((ushort)-31));

                    ClearContainer();

                    break;
            }
        }


        public override void Update()
        {
            base.Update();

            if (_currentPin != null)
            {
                if (Mouse.LDragOffset != Point.Zero && Mouse.LDragOffset != _lastPoint)
                {
                    _currentPin.Location += Mouse.LDragOffset - _lastPoint;

                    if (_currentPin.X < _hit.X)
                    {
                        _currentPin.X = _hit.X;
                    }
                    else if (_currentPin.X >= _hit.Width)
                    {
                        _currentPin.X = _hit.Width;
                    }

                    if (_currentPin.Y < _hit.Y)
                    {
                        _currentPin.Y = _hit.Y;
                    }
                    else if (_currentPin.Y >= _hit.Height)
                    {
                        _currentPin.Y = _hit.Height;
                    }


                    _lastPoint = Mouse.LDragOffset;
                }
            }
        }


        private void TextureControlOnMouseUp(object sender, MouseEventArgs e)
        {
            Point offset = Mouse.LDragOffset;

            if (Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
            {
                if (PlotState != 0 && _currentPin == null && _pinTimer > Time.Ticks)
                {
                    ushort x = (ushort)(e.X + 5);
                    ushort y = (ushort)e.Y;

                    NetClient.Socket.Send_MapMessage(LocalSerial,
                                                     1,
                                                     0,
                                                     x,
                                                     y);

                    AddPin(x, y);
                }
            }

            _currentPin = null;
            _lastPoint = Point.Zero;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.Draw
            (
                _mapTexture,
                new Rectangle(x + _hit.X, y + _hit.Y, _hit.Width, _hit.Height),
                hueVector
            );

            var texture = SolidColorTextureCache.GetTexture(Color.White);

            for (int i = 0; i < _container.Count; i++)
            {
                // HACK: redraw because pins are drawn when calling base.Draw(batcher, x, y);
                _container[i].Draw(batcher, x + _container[i].X, y + _container[i].Y);

                if (i + 1 >= _container.Count)
                {
                    break;
                }

                Control c0 = _container[i];
                Control c1 = _container[i + 1];

                batcher.DrawLine
                (
                    texture,
                    new Vector2(c0.ScreenCoordinateX, c0.ScreenCoordinateY),
                    new Vector2(c1.ScreenCoordinateX, c1.ScreenCoordinateY),
                    hueVector,
                    1
                );
            }

            return true;
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            _currentPin = null;
            _lastPoint = Point.Zero;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            _pinTimer = Time.Ticks + 300;

            if (PlotState != 0 && UIManager.MouseOverControl is PinControl pin)
            {
                _currentPin = pin;
            }
        }


        private int LineUnderMouse(ref int x1, ref int y1, ref int x2, ref int y2)
        {
            int tempX = x2 - x1;
            int tempY = y2 - y1;

            float testOfsX = tempX;

            if (testOfsX == 0.0f)
            {
                testOfsX = 1.0f;
            }

            float pi = (float)Math.PI;

            float a = -(float)(Math.Atan(tempY / testOfsX) * 180f / pi);

            bool inverseCheck = false;

            if (x1 >= x2 && y1 <= y2)
            {
                inverseCheck = true;
            }
            else if (x1 >= x2 && y1 >= y2)
            {
                inverseCheck = true;
            }

            float sinA = (float)Math.Sin(a * pi / 180f);
            float cosA = (float)Math.Sin(a * pi / 180f);

            int offsetX = (int)(tempX * cosA - tempY * sinA);
            int offsetY = (int)(tempX * sinA + tempY * cosA);

            int endX2 = x1 + offsetX;
            int endY2 = y1 + offsetY;

            tempX = Mouse.Position.X - x1; // TODO: must be position relative to the gump
            tempY = Mouse.Position.Y - y1;

            offsetX = (int)(tempX * cosA - tempY * sinA);
            offsetY = (int)(tempX * sinA + tempY * cosA);

            Point mousePoint = new Point(x1 + offsetX, y1 + offsetY);

            const int POLY_OFFSET = 5;

            int result = 0;


            if (!inverseCheck)
            {
                Rectangle rect = new Rectangle
                {
                    X = x1 - POLY_OFFSET,
                    Y = y1 - POLY_OFFSET,
                    Width = endX2 + POLY_OFFSET,
                    Height = endY2 + POLY_OFFSET
                };

                if (rect.Contains(mousePoint))
                {
                    x1 = x1 + (x2 - x1) / 2;
                    y1 = y1 + (y2 - y1) / 2;
                    result = 1;
                }
            }
            else
            {
                Rectangle rect = new Rectangle
                {
                    X = endX2 - POLY_OFFSET,
                    Y = endY2 - POLY_OFFSET,
                    Width = x1 + POLY_OFFSET,
                    Height = x2 + POLY_OFFSET
                };

                if (rect.Contains(mousePoint))
                {
                    x1 = x2 + (x1 - x2) / 2;
                    y1 = y2 + (y1 - y2) / 2;
                    result = 2;
                }
            }

            return result;
        }

        public override void AfterDispose()
        {
            base.AfterDispose();
            if (_hit != null)
            {
                _hit.MouseUp -= TextureControlOnMouseUp;
            }
        }

        private enum ButtonType
        {
            PlotCourse,
            StopPlotting,
            ClearCourse
        }

        private class PinControl : Control
        {
            private readonly GumpPic _pic;
            private readonly RenderedText _text;

            public PinControl(int x, int y)
            {
                X = x;
                Y = y;


                _text = RenderedText.Create(string.Empty, font: 0, isunicode: false);

                _pic = new GumpPic(0, 0, 0x139B, 0);
                Add(_pic);

                WantUpdateSize = false;
                Width = _pic.Width;
                Height = _pic.Height;

                AcceptMouseInput = true;
                CanMove = false;

                _pic.AcceptMouseInput = true;

                Priority = ClickPriority.High;
            }


            //public override bool Contains(int x, int y)
            //{
            //    //x = Mouse.Position.X - ScreenCoordinateX;
            //    //y = Mouse.Position.Y - ScreenCoordinateY;

            //    return _pic.Contains(x, y);
            //}

            public string NumberText
            {
                get => _text.Text;
                set => _text.Text = value;
            }


            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (MouseIsOver)
                {
                    _pic.Hue = 0x35;
                }
                else if (_pic.Hue != 0)
                {
                    _pic.Hue = 0;
                }

                base.Draw(batcher, x, y);
                _text.Draw(batcher, x - _text.Width - 1, y);

                return true;
            }

            public override void Dispose()
            {
                _text?.Destroy();

                base.Dispose();
            }
        }
    }
}