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
using System.Collections.Generic;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Mouse = ClassicUO.Input.Mouse;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MapGump : Gump
    {
        private readonly Button[] _buttons = new Button[3];
        private readonly List<Control> _container = new List<Control>();
        private readonly TextureControl _textureControl;

        public MapGump(Serial serial, ushort gumpid, int width, int height) : base(serial, 0)
        {
            AcceptMouseInput = false;
            CanMove = true;

            Width = width;
            Height = height;

            Add(new ResizePic(0x1432)
            {
                Width = width + 44, Height = height + 61,
            });


            Add(_buttons[0] = new Button((int) ButtonType.PlotCourse, 0x1398, 0x1398) {X = (width - 100) >> 1, Y = 5, ButtonAction = ButtonAction.Activate});
            Add(_buttons[1] = new Button((int) ButtonType.StopPlotting, 0x1399, 0x1399) {X = (width - 70) >> 1, Y = 5, ButtonAction = ButtonAction.Activate});
            Add(_buttons[2] = new Button((int) ButtonType.ClearCourse, 0x139A, 0x139A) {X = (width - 66) >> 1, Y = height + 37, ButtonAction = ButtonAction.Activate});

            _buttons[0].IsVisible = _buttons[0].IsEnabled = PlotState == 0;
            _buttons[1].IsVisible = _buttons[1].IsEnabled = PlotState == 1;
            _buttons[2].IsVisible = _buttons[2].IsEnabled = PlotState == 1;

            Add(_textureControl = new TextureControl
            {
                X = 24, Y = 31,
                Width = width,
                Height = height
            });

            _textureControl.MouseUp += TextureControlOnMouseUp;

            Add(new GumpPic(width - 20, height - 20, 0x0139D, 0));
        }

      
        public int PlotState { get; private set; }

        public void SetMapTexture(SpriteTexture texture)
        {
            _textureControl.Texture?.Dispose();
            _textureControl.WantUpdateSize = true;
            _textureControl.Texture = texture;

            WantUpdateSize = true;
        }

        public void AddToContainer(Control c)
        {
            int x = c.X + c.Width + 5;
            int y = c.Y + c.Height;

            c.X = x;
            c.Y = y;

            _container.Add(c);
            Add(c);
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
            ButtonType b = (ButtonType) buttonID;

            switch (b)
            {
                case ButtonType.PlotCourse:
                case ButtonType.StopPlotting:
                    NetClient.Socket.Send(new PMapMessage(LocalSerial, 6, (byte) PlotState, unchecked((ushort)(-24)), unchecked((ushort)(-31))));
                    SetPlotState(PlotState == 0 ? 1 : 0);
                    break;
                case ButtonType.ClearCourse:
                    NetClient.Socket.Send(new PMapMessage(LocalSerial, 5, 0, unchecked((ushort)(-24)), unchecked((ushort)(-31))));
                    ClearContainer();
                    break;
            }
        }

        private void TextureControlOnMouseUp(object sender, MouseEventArgs e)
        {
            if (_container.Count != 0)
            {

            }
        }


        private int LineUnderMouse(ref int x1, ref int y1, ref int x2, ref int y2)
        {
            int tempX = x2 - x1;
            int tempY = y2 - y1;

            float testOfsX = tempX;

            if (testOfsX == 0.0f)
                testOfsX = 1.0f;

            float pi = (float) Math.PI;

            float a = -(float) (Math.Atan(tempY / testOfsX) * 180f / pi);

            bool inverseCheck = false;

            if (x1 >= x2 && y1 <= y2)
                inverseCheck = true;
            else if (x1 >= x2 && y1 >= y2)
                inverseCheck = true;

            float sinA = (float) Math.Sin(a * pi / 180f);
            float cosA = (float) Math.Sin(a * pi / 180f);

            int offsetX = (int) ((tempX * cosA) - (tempY * sinA));
            int offsetY = (int) ((tempX * sinA) + (tempY * cosA));

            int endX2 = x1 + offsetX;
            int endY2 = y1 + offsetY;

            tempX = Mouse.Position.X - x1; // TODO: must be position relative to the gump
            tempY = Mouse.Position.Y - y1;

            offsetX = (int)((tempX * cosA) - (tempY * sinA));
            offsetY = (int)((tempX * sinA) + (tempY * cosA));

            Point mousePoint = new Point(x1 + offsetX, y1 + offsetY);

            const int POLY_OFFSET = 5;

            int result = 0;


            if (!inverseCheck)
            {
                Rectangle rect = new Rectangle()
                {
                    X = x1 - POLY_OFFSET,
                    Y = y1 - POLY_OFFSET,
                    Width = endX2 + POLY_OFFSET,
                    Height = endY2 + POLY_OFFSET
                };

                if (rect.Contains(mousePoint))
                {
                    x1 = x1 + ((x2 - x1) / 2);
                    y1 = y1 + ((y2 - y1) / 2);
                    result = 1;
                }
            }
            else
            {
                Rectangle rect = new Rectangle()
                {
                    X = endX2 - POLY_OFFSET,
                    Y = endY2 - POLY_OFFSET,
                    Width = x1 + POLY_OFFSET,
                    Height = x2 + POLY_OFFSET
                };

                if (rect.Contains(mousePoint))
                {
                    x1 = x2 + ((x1 - x2) / 2);
                    y1 = y2 + ((y1 - y2) / 2);
                    result = 2;
                }
            }

            return result;
        }

        private enum ButtonType
        {
            PlotCourse,
            StopPlotting,
            ClearCourse
        }

        public override void Dispose()
        {
            _textureControl.MouseUp -= TextureControlOnMouseUp;

            base.Dispose();
        }
    }
}