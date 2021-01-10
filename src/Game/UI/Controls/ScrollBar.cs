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

using System;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScrollBar : ScrollBarBase
    {
        private Rectangle _rectSlider, _emptySpace;
        private readonly UOTexture[] _textureBackground;
        private readonly UOTexture[] _textureDownButton;
        private readonly UOTexture _textureSlider;
        private readonly UOTexture[] _textureUpButton;

        public ScrollBar(int x, int y, int height)
        {
            Height = height;
            Location = new Point(x, y);
            AcceptMouseInput = true;


            _textureUpButton = new UOTexture[2];
            _textureUpButton[0] = GumpsLoader.Instance.GetTexture(251);
            _textureUpButton[1] = GumpsLoader.Instance.GetTexture(250);
            _textureDownButton = new UOTexture[2];
            _textureDownButton[0] = GumpsLoader.Instance.GetTexture(253);
            _textureDownButton[1] = GumpsLoader.Instance.GetTexture(252);
            _textureBackground = new UOTexture[3];
            _textureBackground[0] = GumpsLoader.Instance.GetTexture(257);
            _textureBackground[1] = GumpsLoader.Instance.GetTexture(256);
            _textureBackground[2] = GumpsLoader.Instance.GetTexture(255);
            _textureSlider = GumpsLoader.Instance.GetTexture(254);

            Width = _textureBackground[0].Width;


            _rectDownButton = new Rectangle(0, Height - _textureDownButton[0].Height, _textureDownButton[0].Width, _textureDownButton[0].Height);

            _rectUpButton = new Rectangle(0, 0, _textureUpButton[0].Width, _textureUpButton[0].Height);

            _rectSlider = new Rectangle((_textureBackground[0].Width - _textureSlider.Width) >> 1, _textureUpButton[0].Height + _sliderPosition, _textureSlider.Width, _textureSlider.Height);

            _emptySpace.X = 0;

            _emptySpace.Y = _textureUpButton[0].Height;

            _emptySpace.Width = _textureSlider.Width;

            _emptySpace.Height = Height - (_textureDownButton[0].Height + _textureUpButton[0].Height);
        }


        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);


            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    _textureSlider.Ticks = (long) totalTime;
                }

                if (i < 2)
                {
                    _textureUpButton[i].Ticks = (long) totalTime;

                    _textureDownButton[i].Ticks = (long) totalTime;
                }

                _textureBackground[i].Ticks = (long) totalTime;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Height <= 0 || !IsVisible)
            {
                return false;
            }

            ResetHueVector();

            // draw scrollbar background
            int middleHeight = Height - _textureUpButton[0].Height - _textureDownButton[0].Height - _textureBackground[0].Height - _textureBackground[2].Height;

            if (middleHeight > 0)
            {
                batcher.Draw2D(_textureBackground[0], x, y + _textureUpButton[0].Height, ref HueVector);

                batcher.Draw2DTiled
                (
                    _textureBackground[1],
                    x,
                    y + _textureUpButton[0].Height + _textureBackground[0].Height,
                    _textureBackground[0].Width,
                    middleHeight,
                    ref HueVector
                );

                batcher.Draw2D(_textureBackground[2], x, y + Height - _textureDownButton[0].Height - _textureBackground[2].Height, ref HueVector);
            }
            else
            {
                middleHeight = Height - _textureUpButton[0].Height - _textureDownButton[0].Height;

                batcher.Draw2DTiled
                (
                    _textureBackground[1],
                    x,
                    y + _textureUpButton[0].Height,
                    _textureBackground[0].Width,
                    middleHeight,
                    ref HueVector
                );
            }

            // draw up button
            batcher.Draw2D(_btUpClicked ? _textureUpButton[1] : _textureUpButton[0], x, y, ref HueVector);

            // draw down button
            batcher.Draw2D(_btDownClicked ? _textureDownButton[1] : _textureDownButton[0], x, y + Height - _textureDownButton[0].Height, ref HueVector);

            // draw slider
            if (MaxValue > MinValue && middleHeight > 0)
            {
                batcher.Draw2D(_textureSlider, x + ((_textureBackground[0].Width - _textureSlider.Width) >> 1), y + _textureUpButton[0].Height + _sliderPosition, ref HueVector);
            }

            return base.Draw(batcher, x, y);
        }

        protected override int GetScrollableArea()
        {
            return Height - _textureUpButton[0].Height - _textureDownButton[0].Height - _textureSlider.Height;
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            base.OnMouseDown(x, y, button);

            if (_btnSliderClicked && _emptySpace.Contains(x, y))
            {
                CalculateByPosition(x, y);
            }
        }

        protected override void CalculateByPosition(int x, int y)
        {
            if (y != _clickPosition.Y)
            {
                y -= _emptySpace.Y + (_rectSlider.Height >> 1);

                if (y < 0)
                {
                    y = 0;
                }

                int scrollableArea = GetScrollableArea();

                if (y > scrollableArea)
                {
                    y = scrollableArea;
                }

                _sliderPosition = y;
                _clickPosition.X = x;
                _clickPosition.Y = y;

                if (y == 0 && _clickPosition.Y < _textureUpButton[0].Height + (_textureSlider.Height >> 1))
                {
                    _clickPosition.Y = _textureUpButton[0].Height + (_textureSlider.Height >> 1);
                }
                else if (y == scrollableArea && _clickPosition.Y > Height - _textureDownButton[0].Height - (_textureSlider.Height >> 1))
                {
                    _clickPosition.Y = Height - _textureDownButton[0].Height - (_textureSlider.Height >> 1);
                }

                _value = (int) Math.Round(y / (float) scrollableArea * (MaxValue - MinValue) + MinValue);
            }
        }

        public override bool Contains(int x, int y)
        {
            return x >= 0 && x <= Width && y >= 0 && y <= Height;
        }
    }
}