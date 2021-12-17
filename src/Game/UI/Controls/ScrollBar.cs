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

        const ushort BUTTON_UP_0 = 251;
        const ushort BUTTON_UP_1 = 250;
        const ushort BUTTON_DOWN_0 = 253;
        const ushort BUTTON_DOWN_1 = 252;
        const ushort BACKGROUND_0 = 257;
        const ushort BACKGROUND_1 = 256;
        const ushort BACKGROUND_2 = 255;
        const ushort SLIDER = 254;

        public ScrollBar(int x, int y, int height)
        {
            Height = height;
            Location = new Point(x, y);
            AcceptMouseInput = true;


            _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_UP_0, out var boundsUp0);
            _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_DOWN_0, out var boundsDown0);
            _ = GumpsLoader.Instance.GetGumpTexture(BACKGROUND_0, out var boundsBackground0);
            _ = GumpsLoader.Instance.GetGumpTexture(SLIDER, out var boundsSlider);


            Width = boundsBackground0.Width;

            _rectDownButton = new Rectangle(0, Height - boundsDown0.Height, boundsDown0.Width, boundsDown0.Height);
            _rectUpButton = new Rectangle(0, 0, boundsUp0.Width, boundsUp0.Height);
            _rectSlider = new Rectangle((boundsBackground0.Width - boundsSlider.Width) >> 1, boundsUp0.Height + _sliderPosition, boundsSlider.Width, boundsSlider.Height);
            _emptySpace.X = 0;
            _emptySpace.Y = boundsUp0.Height;
            _emptySpace.Width = boundsSlider.Width;
            _emptySpace.Height = Height - (boundsDown0.Height + boundsUp0.Height);
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Height <= 0 || !IsVisible)
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            var textureUp0 = GumpsLoader.Instance.GetGumpTexture(BUTTON_UP_0, out var boundsUp0);
            var textureUp1 = GumpsLoader.Instance.GetGumpTexture(BUTTON_UP_1, out var boundsUp1);
            var textureDown0 = GumpsLoader.Instance.GetGumpTexture(BUTTON_DOWN_0, out var boundsDown0);
            var textureDown1 = GumpsLoader.Instance.GetGumpTexture(BUTTON_DOWN_1, out var boundsDown1);
            var textureBackground0 = GumpsLoader.Instance.GetGumpTexture(BACKGROUND_0, out var boundsBackground0);
            var textureBackground1 = GumpsLoader.Instance.GetGumpTexture(BACKGROUND_1, out var boundsBackground1);
            var textureBackground2 = GumpsLoader.Instance.GetGumpTexture(BACKGROUND_2, out var boundsBackground2);
            var textureSlider = GumpsLoader.Instance.GetGumpTexture(SLIDER, out var boundsSlider);

            // draw scrollbar background
            int middleHeight = Height - boundsUp0.Height - boundsDown0.Height - boundsBackground0.Height - boundsBackground2.Height;

            if (middleHeight > 0)
            {
                batcher.Draw
                (
                    textureBackground0,
                    new Vector2(x, y + boundsUp0.Height),
                    boundsBackground0,
                    hueVector
                );

                batcher.DrawTiled
                (
                    textureBackground1,
                    new Rectangle
                    (
                        x,
                        y + boundsUp1.Height + boundsBackground0.Height,
                        boundsBackground0.Width,
                        middleHeight
                    ),
                    boundsBackground1,
                    hueVector
                );

                batcher.Draw
                (
                    textureBackground2,
                    new Vector2(x, y + Height - boundsDown0.Height - boundsBackground2.Height),
                    boundsBackground2,
                    hueVector
                );
            }
            else
            {
                middleHeight = Height - boundsUp0.Height - boundsDown0.Height;

                batcher.DrawTiled
                (
                    textureBackground1,
                    new Rectangle
                    (
                        x,
                        y + boundsUp0.Height,
                        boundsBackground0.Width,
                        middleHeight
                    ),
                    boundsBackground1,
                    hueVector
                );
            }

            // draw up button
            if (_btUpClicked)
            {
                batcher.Draw
                (
                    textureUp1,
                    new Vector2(x, y),
                    boundsUp1,
                    hueVector
                );
            }
            else
            {
                batcher.Draw
                (
                    textureUp0,
                    new Vector2(x, y),
                    boundsUp0,
                    hueVector
                );
            }

            // draw down button
            if (_btDownClicked)
            {
                batcher.Draw
                (
                    textureDown1,
                    new Vector2(x, y + Height - boundsDown0.Height),
                    boundsDown1,
                    hueVector
                );
            }
            else
            {
                batcher.Draw
                (
                    textureDown0,
                    new Vector2(x, y + Height - boundsDown0.Height),
                    boundsDown0,
                    hueVector
                );
            }        

            // draw slider
            if (MaxValue > MinValue && middleHeight > 0)
            {
                batcher.Draw
                (
                    textureSlider,
                    new Vector2
                    (
                        x + ((boundsBackground0.Width - boundsSlider.Width) >> 1), 
                        y + boundsUp0.Height + _sliderPosition
                    ),
                    boundsSlider,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }

        protected override int GetScrollableArea()
        {
            _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_UP_0, out var boundsUp0);
            _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_DOWN_0, out var boundsDown0);
            _ = GumpsLoader.Instance.GetGumpTexture(SLIDER, out var boundsSlider);

            return Height - boundsUp0.Height - boundsDown0.Height - boundsSlider.Height;
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

                _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_UP_0, out var boundsUp0);
                _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_DOWN_0, out var boundsDown0);
                _ = GumpsLoader.Instance.GetGumpTexture(SLIDER, out var boundsSlider);

                if (y == 0 && _clickPosition.Y < boundsUp0.Height + (boundsSlider.Height >> 1))
                {
                    _clickPosition.Y = boundsUp0.Height + (boundsSlider.Height >> 1);
                }
                else if (y == scrollableArea && _clickPosition.Y > Height - boundsDown0.Height - (boundsSlider.Height >> 1))
                {
                    _clickPosition.Y = Height - boundsDown0.Height - (boundsSlider.Height >> 1);
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