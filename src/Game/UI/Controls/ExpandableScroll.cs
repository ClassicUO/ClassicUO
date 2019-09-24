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

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ExpandableScroll : Control
    {
        private const int c_ExpandableScrollHeight_Min = 274;
        private const int c_ExpandableScrollHeight_Max = 1000;
        private const int c_GumplingExpanderY_Offset = 2; // this is the gap between the pixels of the btm Control texture and the height of the btm Control texture.
        private const int c_GumplingExpander_ButtonID = 0x7FBEEF;
        private readonly bool _isResizable = true;
        private Button _gumpExpander;
        private GumpPic _gumplingTitle;
        private int _gumplingTitleGumpID;
        private bool _gumplingTitleGumpIDDelta;
        private readonly GumpPicTiled _gumpMiddle;
        private readonly GumpPicTiled _gumpRight;
        private readonly GumpPic _gumpTop;
        private readonly GumpPic _gumpBottom;
        private bool _isExpanding;
        private int _isExpanding_InitialX, _isExpanding_InitialY, _isExpanding_InitialHeight;

        private readonly int _maxWidth;

        public ExpandableScroll(int x, int y, int height, ushort graphic, bool isResizable = true)
        {
            X = x;
            Y = y;
            SpecialHeight = height;
            _isResizable = isResizable;
            CanMove = true;
            AcceptMouseInput = true;

            var textures = new UOTexture[4];

            for (int i = 0; i < 4; i++)
            {
                var t = FileManager.Gumps.GetTexture((ushort) (graphic + i));

                if (t == null)
                {
                    Dispose();

                    return;
                }

                if (t.Width > _maxWidth)
                    _maxWidth = t.Width;

                textures[i] = t;
            }


            Add(_gumpTop = new GumpPic(0, 0, graphic, 0));
            Add(_gumpRight = new GumpPicTiled(0, 0, 0, 0, (ushort) (graphic + 1)));
            Add(_gumpMiddle = new GumpPicTiled(0, 0, 0, 0, (ushort) (graphic + 2)));
            Add(_gumpBottom = new GumpPic(0, 0, (ushort) (graphic + 3), 0));

            if (_isResizable)
            {
                Add(_gumpExpander = new Button(c_GumplingExpander_ButtonID, 0x082E, 0x82F)
                {
                    ButtonAction = ButtonAction.Activate,
                    X = 0,
                    Y = 0
                });
                _gumpExpander.MouseDown += expander_OnMouseDown;
                _gumpExpander.MouseUp += expander_OnMouseUp;
                _gumpExpander.MouseOver += expander_OnMouseOver;
            }

            int off = textures[0].Width - textures[3].Width;
            _maxWidth = textures[1].Width;

            _gumpRight.X = _gumpMiddle.X = 17;
            _gumpRight.X = _gumpMiddle.Y = _gumplingMidY;
            _gumpRight.Width = _gumpMiddle.Width = _maxWidth;
            _gumpRight.Height = _gumpMiddle.Height = _gumplingMidHeight;
            _gumpRight.WantUpdateSize = _gumpMiddle.WantUpdateSize = true;

            Width = _gumpMiddle.Width;


            WantUpdateSize = true;
        }

        private int _gumplingMidY => _gumpTop.Height;

        private int _gumplingMidHeight => SpecialHeight - _gumpTop.Height - _gumpBottom.Height - (_gumpExpander?.Height ?? 0);

        private int _gumplingBottomY => SpecialHeight - _gumpBottom.Height - (_gumpExpander?.Height ?? 0);

        private int _gumplingExpanderX => (Width - (_gumpExpander?.Width ?? 0)) >> 1;

        private int _gumplingExpanderY => SpecialHeight - (_gumpExpander?.Height ?? 0) - c_GumplingExpanderY_Offset;

        public int TitleGumpID
        {
            set
            {
                _gumplingTitleGumpID = value;
                _gumplingTitleGumpIDDelta = true;
            }
        }

        public int SpecialHeight { get; set; }

        public Hue Hue
        {
            get => _gumpTop.Hue;
            set => _gumpTop.Hue = _gumpBottom.Hue = _gumpMiddle.Hue = _gumpRight.Hue = value;
        }

        public override void Dispose()
        {
            if (_gumpExpander != null)
            {
                _gumpExpander.MouseDown -= expander_OnMouseDown;
                _gumpExpander.MouseUp -= expander_OnMouseUp;
                _gumpExpander.MouseOver -= expander_OnMouseOver;
                _gumpExpander.Dispose();
                _gumpExpander = null;
            }

            base.Dispose();
        }

        public override bool Contains(int x, int y)
        {
            x += ScreenCoordinateX;
            y += ScreenCoordinateY;

            if (_gumpTop.HitTest(x, y) != null)
                return true;

            if (_gumpMiddle.HitTest(x, y) != null)
                return true;

            if (_gumpRight.HitTest(x, y) != null)
                return true;

            if (_gumpBottom.HitTest(x, y) != null)
                return true;

            if (_isResizable && _gumpExpander.HitTest(x, y) != null)
                return true;

            return false;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (SpecialHeight < c_ExpandableScrollHeight_Min)
                SpecialHeight = c_ExpandableScrollHeight_Min;

            if (SpecialHeight > c_ExpandableScrollHeight_Max)
                SpecialHeight = c_ExpandableScrollHeight_Max;

            if (_gumplingTitleGumpIDDelta)
            {
                _gumplingTitleGumpIDDelta = false;

                _gumplingTitle?.Dispose();
                Add(_gumplingTitle = new GumpPic(0, 0, (Graphic) _gumplingTitleGumpID, 0));
            }

            if (!_gumpTop.IsInitialized)
                IsVisible = false;
            else
            {
                if (!IsVisible)
                    IsVisible = true;
                //TOP
                _gumpTop.X = 0;
                _gumpTop.Y = 0;
                _gumpTop.WantUpdateSize = true;
                //MIDDLE
                _gumpRight.X = _gumpMiddle.X = 17;
                _gumpRight.Y = _gumpMiddle.Y = _gumplingMidY;
                _gumpRight.Width = _gumpMiddle.Width = _maxWidth;
                _gumpRight.Height = _gumpMiddle.Height = _gumplingMidHeight;
                _gumpRight.WantUpdateSize = _gumpMiddle.WantUpdateSize = true;
                //BOTTOM
                _gumpBottom.X = 17;
                _gumpBottom.Y = _gumplingBottomY;
                _gumpBottom.WantUpdateSize = true;

                if (_isResizable)
                {
                    _gumpExpander.X = _gumplingExpanderX;
                    _gumpExpander.Y = _gumplingExpanderY;
                    _gumpExpander.WantUpdateSize = true;
                }

                if (_gumplingTitle != null && _gumplingTitle.IsInitialized)
                {
                    _gumplingTitle.X = (_gumpTop.Width - _gumplingTitle.Width) >> 1;
                    _gumplingTitle.Y = (_gumpTop.Height - _gumplingTitle.Height) >> 1;
                    _gumplingTitle.WantUpdateSize = true;
                }

                WantUpdateSize = true;
                Parent?.OnPageChanged();
            }

            base.Update(totalMS, frameMS);
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
                _isExpanding_InitialHeight = SpecialHeight;
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
                SpecialHeight = _isExpanding_InitialHeight + (y - _isExpanding_InitialY);
            }
        }

        private void expander_OnMouseOver(object sender, MouseEventArgs args)
        {
            int y = args.Y;
            y += _gumpExpander.Y + ScreenCoordinateY - Y;
            if (_isExpanding && y != _isExpanding_InitialY) SpecialHeight = _isExpanding_InitialHeight + (y - _isExpanding_InitialY);
        }
    }
}