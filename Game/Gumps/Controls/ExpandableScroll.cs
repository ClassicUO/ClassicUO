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

using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class ExpandableScroll : GumpControl
    {
        private const int c_ExpandableScrollHeight_Min = 274;
        private const int c_ExpandableScrollHeight_Max = 1000;
        private const int c_GumplingExpanderY_Offset = 2; // this is the gap between the pixels of the btm Control texture and the height of the btm Control texture.
        private const int c_GumplingExpander_ButtonID = 0x7FBEEF;
        private readonly bool _isResizable = true;
        private int _expandableScrollHeight;
        private Button _gumpExpander;
        private GumpPic _gumplingTitle;
        private int _gumplingTitleGumpID;
        private bool _gumplingTitleGumpIDDelta;
        private GumpPicTiled _gumpMiddle;
        private GumpPic _gumpTop, _gumpBottom;
        private bool _isExpanding;
        private int _isExpanding_InitialX, _isExpanding_InitialY, _isExpanding_InitialHeight;

        public ExpandableScroll(int x, int y, int height, bool isResizable = true)
        {
            X = x;
            Y = y;
            _expandableScrollHeight = height;
            _isResizable = isResizable;
            CanMove = true;
        }

        private int _gumplingMidY => _gumpTop.Height;

        private int _gumplingMidHeight => _expandableScrollHeight - _gumpTop.Height - _gumpBottom.Height - (_gumpExpander != null ? _gumpExpander.Height : 0);

        private int _gumplingBottomY => _expandableScrollHeight - _gumpBottom.Height - (_gumpExpander != null ? _gumpExpander.Height : 0);

        private int _gumplingExpanderX => (Width - (_gumpExpander != null ? _gumpExpander.Width : 0)) / 2;

        private int _gumplingExpanderY => _expandableScrollHeight - (_gumpExpander != null ? _gumpExpander.Height : 0) - c_GumplingExpanderY_Offset;

        public int TitleGumpID
        {
            set
            {
                _gumplingTitleGumpID = value;
                _gumplingTitleGumpIDDelta = true;
            }
        }

        protected override void OnInitialize()
        {
            AddChildren(_gumpTop = new GumpPic(0, 0, 0x0820, 0));
            AddChildren(_gumpMiddle = new GumpPicTiled(0, 0, 0, 0, 0x0822));
            AddChildren(_gumpBottom = new GumpPic(0, 0, 0x0823, 0));

            if (_isResizable)
            {
                AddChildren(_gumpExpander = new Button(c_GumplingExpander_ButtonID, 0x082E, 0x82F)
                {
                    ButtonAction = ButtonAction.Activate, X = 0, Y = 0
                });
                _gumpExpander.MouseDown += expander_OnMouseDown;
                _gumpExpander.MouseUp += expander_OnMouseUp;
                _gumpExpander.MouseEnter += expander_OnMouseOver;
            }
        }

        public override void Dispose()
        {
            if (_gumpExpander != null)
            {
                _gumpExpander.MouseDown -= expander_OnMouseDown;
                _gumpExpander.MouseUp -= expander_OnMouseUp;
                _gumpExpander.MouseEnter -= expander_OnMouseOver;
                _gumpExpander.Dispose();
                _gumpExpander = null;
            }

            base.Dispose();
        }

        protected override bool Contains(int x, int y)
        {
            Point position = new Point(x + ScreenCoordinateX, y + ScreenCoordinateY);

            if (_gumpTop.HitTest(position) != null)
                return true;

            if (_gumpMiddle.HitTest(position) != null)
                return true;

            if (_gumpBottom.HitTest(position) != null)
                return true;

            if (_isResizable && _gumpExpander.HitTest(position) != null)
                return true;

            return false;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_expandableScrollHeight < c_ExpandableScrollHeight_Min)
                _expandableScrollHeight = c_ExpandableScrollHeight_Min;

            if (_expandableScrollHeight > c_ExpandableScrollHeight_Max)
                _expandableScrollHeight = c_ExpandableScrollHeight_Max;

            if (_gumplingTitleGumpIDDelta)
            {
                _gumplingTitleGumpIDDelta = false;

                if (_gumplingTitle != null)
                    _gumplingTitle.Dispose();
                AddChildren(_gumplingTitle = new GumpPic(0, 0, (Graphic) _gumplingTitleGumpID, 0));
            }

            if (!_gumpTop.IsInitialized)
                IsVisible = false;
            else
            {
                IsVisible = true;
                //TOP
                _gumpTop.X = 0;
                _gumpTop.Y = 0;
                //MIDDLE
                _gumpMiddle.X = 17;
                _gumpMiddle.Y = _gumplingMidY;
                _gumpMiddle.Width = 263;
                _gumpMiddle.Height = _gumplingMidHeight;
                //BOTTOM
                _gumpBottom.X = 17;
                _gumpBottom.Y = _gumplingBottomY;

                if (_isResizable)
                {
                    _gumpExpander.X = _gumplingExpanderX;
                    _gumpExpander.Y = _gumplingExpanderY;
                }

                if (_gumplingTitle != null && _gumplingTitle.IsInitialized)
                {
                    _gumplingTitle.X = (_gumpTop.Width - _gumplingTitle.Width) / 2;
                    _gumplingTitle.Y = (_gumpTop.Height - _gumplingTitle.Height) / 2;
                }
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            return base.Draw(spriteBatch, position, hue);
        }

        //new MouseEventArgs(x, y, button, ButtonState.Pressed)
        private void expander_OnMouseDown(object sender, MouseEventArgs args)
        {
            int y = args.Y;
            int x = args.X;
            y += _gumpExpander.Y + ScreenCoordinateY - Y;

            if (args.Button == MouseButton.Left)
            {
                _isExpanding = true;
                _isExpanding_InitialHeight = _expandableScrollHeight;
                _isExpanding_InitialX = x;
                _isExpanding_InitialY = y;
            }
        }

        private void expander_OnMouseUp(object sender, MouseEventArgs args)
        {
            int y = args.Y;
            y += _gumpExpander.Y + ScreenCoordinateY - Y;

            if (_isExpanding)
            {
                _isExpanding = false;
                _expandableScrollHeight = _isExpanding_InitialHeight + (y - _isExpanding_InitialY);
            }
        }

        private void expander_OnMouseOver(object sender, MouseEventArgs args)
        {
            int y = args.Y;
            y += _gumpExpander.Y + ScreenCoordinateY - Y;
            if (_isExpanding && y != _isExpanding_InitialY) _expandableScrollHeight = _isExpanding_InitialHeight + (y - _isExpanding_InitialY);
        }
    }
}