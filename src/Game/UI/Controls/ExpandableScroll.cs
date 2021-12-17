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

using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    internal class ExpandableScroll : Control
    {
        private const int c_ExpandableScrollHeight_Min = 274;
        private const int c_ExpandableScrollHeight_Max = 1000;
        private const int c_GumplingExpanderY_Offset = 2; // this is the gap between the pixels of the btm Control texture and the height of the btm Control texture.
        private const int c_GumplingExpander_ButtonID = 0x7FBEEF;
        private readonly GumpPic _gumpBottom;
        private Button _gumpExpander;
        private GumpPic _gumplingTitle;
        private int _gumplingTitleGumpID;
        private bool _gumplingTitleGumpIDDelta;
        private readonly GumpPicTiled _gumpMiddle;
        private readonly GumpPicTiled _gumpRight;
        private readonly GumpPic _gumpTop;
        private bool _isExpanding;
        private int _isExpanding_InitialX, _isExpanding_InitialY, _isExpanding_InitialHeight;
        private readonly bool _isResizable = true;

        public ExpandableScroll(int x, int y, int height, ushort graphic, bool isResizable = true)
        {
            X = x;
            Y = y;
            SpecialHeight = height;
            _isResizable = isResizable;
            CanMove = true;
            AcceptMouseInput = true;

            int width = 0;

            int w0 = 0, w1 = 0, w3 = 0;

            for (int i = 0; i < 4; i++)
            {
                var texture = GumpsLoader.Instance.GetGumpTexture((ushort) (graphic + i), out var bounds);

                if (texture == null)
                {
                    Dispose();

                    return;
                }

                if (bounds.Width > width)
                {
                    width = bounds.Width;
                }

                if (i == 0)
                {
                    w0 = bounds.Width;
                }
                else if (i == 1)
                {
                    w1 = bounds.Width;
                }
                else if (i == 3)
                {
                    w3 = bounds.Width;
                }
            }


            Add(_gumpTop = new GumpPic(0, 0, graphic, 0));

            Add
            (
                _gumpRight = new GumpPicTiled
                (
                    0,
                    0,
                    0,
                    0,
                    (ushort) (graphic + 1)
                )
            );

            Add
            (
                _gumpMiddle = new GumpPicTiled
                (
                    0,
                    0,
                    0,
                    0,
                    (ushort) (graphic + 2)
                )
            );

            Add(_gumpBottom = new GumpPic(0, 0, (ushort) (graphic + 3), 0));

            if (_isResizable)
            {
                Add
                (
                    _gumpExpander = new Button(c_GumplingExpander_ButtonID, 0x082E, 0x82F)
                    {
                        ButtonAction = ButtonAction.Activate,
                        X = 0,
                        Y = 0
                    }
                );

                _gumpExpander.MouseDown += expander_OnMouseDown;
                _gumpExpander.MouseUp += expander_OnMouseUp;
                _gumpExpander.MouseOver += expander_OnMouseOver;
            }

            int off = w0 - w3;

            _gumpRight.X = _gumpMiddle.X = (width - w1) / 2;
            _gumpRight.Y = _gumpMiddle.Y = _gumplingMidY;
            _gumpRight.Height = _gumpMiddle.Height = _gumplingMidHeight;
            _gumpRight.WantUpdateSize = _gumpMiddle.WantUpdateSize = true;
            _gumpBottom.X = (off / 2) + (off / 4);

            Width = _gumpMiddle.Width;


            WantUpdateSize = true;
        }

        private int _gumplingMidY => _gumpTop.Height;

        private int _gumplingMidHeight =>
            SpecialHeight - _gumpTop.Height - _gumpBottom.Height - (_gumpExpander?.Height ?? 0);

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

        public ushort Hue
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

            Control c = null;


            _gumpTop.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            _gumpMiddle.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            _gumpRight.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            _gumpBottom.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            _gumpExpander.HitTest(x, y, ref c);

            if (c != null)
            {
                return true;
            }

            return false;
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (SpecialHeight < c_ExpandableScrollHeight_Min)
            {
                SpecialHeight = c_ExpandableScrollHeight_Min;
            }

            if (SpecialHeight > c_ExpandableScrollHeight_Max)
            {
                SpecialHeight = c_ExpandableScrollHeight_Max;
            }

            if (_gumplingTitleGumpIDDelta)
            {
                _gumplingTitleGumpIDDelta = false;

                _gumplingTitle?.Dispose();
                Add(_gumplingTitle = new GumpPic(0, 0, (ushort) _gumplingTitleGumpID, 0));
            }


            {
                //if (!IsVisible)
                //    IsVisible = true;
                //TOP
                _gumpTop.X = 0;
                _gumpTop.Y = 0;
                _gumpTop.WantUpdateSize = true;
                //MIDDLE
                _gumpRight.Y = _gumpMiddle.Y = _gumplingMidY;
                _gumpRight.Height = _gumpMiddle.Height = _gumplingMidHeight;
                _gumpRight.WantUpdateSize = _gumpMiddle.WantUpdateSize = true;
                //BOTTOM
                _gumpBottom.Y = _gumplingBottomY;
                _gumpBottom.WantUpdateSize = true;

                if (_isResizable)
                {
                    _gumpExpander.X = _gumplingExpanderX;
                    _gumpExpander.Y = _gumplingExpanderY;
                    _gumpExpander.WantUpdateSize = true;
                }

                if (_gumplingTitle != null)
                {
                    _gumplingTitle.X = (_gumpTop.Width - _gumplingTitle.Width) >> 1;
                    _gumplingTitle.Y = (_gumpTop.Height - _gumplingTitle.Height) >> 1;
                    _gumplingTitle.WantUpdateSize = true;
                }

                WantUpdateSize = true;
                Parent?.OnPageChanged();
            }

            base.Update(totalTime, frameTime);
        }


        //new MouseEventArgs(x, y, button, ButtonState.Pressed)
        private void expander_OnMouseDown(object sender, MouseEventArgs args)
        {
            int y = args.Y;
            int x = args.X;
            y += _gumpExpander.Y + ScreenCoordinateY - Y;

            if (args.Button == MouseButtonType.Left)
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

            if (_isExpanding && y != _isExpanding_InitialY)
            {
                SpecialHeight = _isExpanding_InitialHeight + (y - _isExpanding_InitialY);
            }
        }
    }
}