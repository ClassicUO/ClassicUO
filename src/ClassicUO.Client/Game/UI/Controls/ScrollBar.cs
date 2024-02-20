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
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScrollBar : ScrollBarBase
    {
        private Rectangle _rectSlider,
            _emptySpace;

        const ushort BUTTON_UP_0 = 251;
        const ushort BUTTON_UP_1 = 250;
        const ushort BUTTON_DOWN_0 = 253;
        const ushort BUTTON_DOWN_1 = 252;
        const ushort BACKGROUND_0 = 257;
        const ushort BACKGROUND_1 = 256;
        const ushort BACKGROUND_2 = 255;
        const ushort SLIDER = 254;

        private Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

        public ScrollBar(int x, int y, int height)
        {
            Height = height;
            Location = new Point(x, y);
            AcceptMouseInput = true;

            ref readonly var gumpInfoUp = ref Client.Game.Gumps.GetGump(BUTTON_UP_0);
            ref readonly var gumpInfoDown = ref Client.Game.Gumps.GetGump(BUTTON_DOWN_0);
            ref readonly var gumpInfoBackground = ref Client.Game.Gumps.GetGump(BACKGROUND_0);
            ref readonly var gumpInfoSlider = ref Client.Game.Gumps.GetGump(SLIDER);

            Width = gumpInfoBackground.UV.Width;

            _rectDownButton = new Rectangle(
                0,
                Height - gumpInfoDown.UV.Height,
                gumpInfoDown.UV.Width,
                gumpInfoDown.UV.Height
            );
            _rectUpButton = new Rectangle(0, 0, gumpInfoUp.UV.Width, gumpInfoUp.UV.Height);
            _rectSlider = new Rectangle(
                (gumpInfoBackground.UV.Width - gumpInfoSlider.UV.Width) >> 1,
                gumpInfoUp.UV.Height + _sliderPosition,
                gumpInfoSlider.UV.Width,
                gumpInfoSlider.UV.Height
            );
            _emptySpace.X = 0;
            _emptySpace.Y = gumpInfoUp.UV.Height;
            _emptySpace.Width = gumpInfoSlider.UV.Width;
            _emptySpace.Height = Height - (gumpInfoDown.UV.Height + gumpInfoUp.UV.Height);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Height <= 0 || !IsVisible)
            {
                return false;
            }

            ref readonly var gumpInfoUp0 = ref Client.Game.Gumps.GetGump(BUTTON_UP_0);
            ref readonly var gumpInfoUp1 = ref Client.Game.Gumps.GetGump(BUTTON_UP_1);
            ref readonly var gumpInfoDown0 = ref Client.Game.Gumps.GetGump(BUTTON_DOWN_0);
            ref readonly var gumpInfoDown1 = ref Client.Game.Gumps.GetGump(BUTTON_DOWN_1);
            ref readonly var gumpInfoBackground0 = ref Client.Game.Gumps.GetGump(BACKGROUND_0);
            ref readonly var gumpInfoBackground1 = ref Client.Game.Gumps.GetGump(BACKGROUND_1);
            ref readonly var gumpInfoBackground2 = ref Client.Game.Gumps.GetGump(BACKGROUND_2);
            ref readonly var gumpInfoSlider = ref Client.Game.Gumps.GetGump(SLIDER);

            // draw scrollbar background
            int middleHeight =
                Height
                - gumpInfoUp0.UV.Height
                - gumpInfoDown0.UV.Height
                - gumpInfoBackground0.UV.Height
                - gumpInfoBackground2.UV.Height;

            if (middleHeight > 0)
            {
                batcher.Draw(
                    gumpInfoBackground0.Texture,
                    new Vector2(x, y + gumpInfoUp0.UV.Height),
                    gumpInfoBackground0.UV,
                    hueVector
                );

                batcher.DrawTiled(
                    gumpInfoBackground1.Texture,
                    new Rectangle(
                        x,
                        y + gumpInfoUp1.UV.Height + gumpInfoBackground0.UV.Height,
                        gumpInfoBackground0.UV.Width,
                        middleHeight
                    ),
                    gumpInfoBackground1.UV,
                    hueVector
                );

                batcher.Draw(
                    gumpInfoBackground2.Texture,
                    new Vector2(
                        x,
                        y + Height - gumpInfoDown0.UV.Height - gumpInfoBackground2.UV.Height
                    ),
                    gumpInfoBackground2.UV,
                    hueVector
                );
            }
            else
            {
                middleHeight = Height - gumpInfoUp0.UV.Height - gumpInfoDown0.UV.Height;

                batcher.DrawTiled(
                    gumpInfoBackground1.Texture,
                    new Rectangle(
                        x,
                        y + gumpInfoUp0.UV.Height,
                        gumpInfoBackground0.UV.Width,
                        middleHeight
                    ),
                    gumpInfoBackground1.UV,
                    hueVector
                );
            }

            // draw up button
            if (_btUpClicked)
            {
                batcher.Draw(gumpInfoUp1.Texture, new Vector2(x, y), gumpInfoUp1.UV, hueVector);
            }
            else
            {
                batcher.Draw(gumpInfoUp0.Texture, new Vector2(x, y), gumpInfoUp0.UV, hueVector);
            }

            // draw down button
            if (_btDownClicked)
            {
                batcher.Draw(
                    gumpInfoDown1.Texture,
                    new Vector2(x, y + Height - gumpInfoDown0.UV.Height),
                    gumpInfoDown1.UV,
                    hueVector
                );
            }
            else
            {
                batcher.Draw(
                    gumpInfoDown0.Texture,
                    new Vector2(x, y + Height - gumpInfoDown0.UV.Height),
                    gumpInfoDown0.UV,
                    hueVector
                );
            }

            // draw slider
            if (MaxValue > MinValue && middleHeight > 0)
            {
                batcher.Draw(
                    gumpInfoSlider.Texture,
                    new Vector2(
                        x + ((gumpInfoBackground0.UV.Width - gumpInfoSlider.UV.Width) >> 1),
                        y + gumpInfoUp0.UV.Height + _sliderPosition
                    ),
                    gumpInfoSlider.UV,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }

        protected override int GetScrollableArea()
        {
            ref readonly var gumpInfoUp = ref Client.Game.Gumps.GetGump(BUTTON_UP_0);
            ref readonly var gumpInfoDown = ref Client.Game.Gumps.GetGump(BUTTON_DOWN_0);
            ref readonly var gumpInfoSlider = ref Client.Game.Gumps.GetGump(SLIDER);

            return Height
                - gumpInfoUp.UV.Height
                - gumpInfoDown.UV.Height
                - gumpInfoSlider.UV.Height;
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

                ref readonly var gumpInfoUp = ref Client.Game.Gumps.GetGump(BUTTON_UP_0);
                ref readonly var gumpInfoDown = ref Client.Game.Gumps.GetGump(BUTTON_DOWN_0);
                ref readonly var gumpInfoSlider = ref Client.Game.Gumps.GetGump(SLIDER);

                if (
                    y == 0
                    && _clickPosition.Y < gumpInfoUp.UV.Height + (gumpInfoSlider.UV.Height >> 1)
                )
                {
                    _clickPosition.Y = gumpInfoUp.UV.Height + (gumpInfoSlider.UV.Height >> 1);
                }
                else if (
                    y == scrollableArea
                    && _clickPosition.Y
                        > Height - gumpInfoDown.UV.Height - (gumpInfoSlider.UV.Height >> 1)
                )
                {
                    _clickPosition.Y =
                        Height - gumpInfoDown.UV.Height - (gumpInfoSlider.UV.Height >> 1);
                }

                _value = (int)
                    Math.Round(y / (float)scrollableArea * (MaxValue - MinValue) + MinValue);
            }
        }

        public override bool Contains(int x, int y)
        {
            return x >= 0 && x <= Width && y >= 0 && y <= Height;
        }
    }
}
