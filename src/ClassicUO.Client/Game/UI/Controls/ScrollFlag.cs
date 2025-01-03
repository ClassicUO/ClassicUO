#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScrollFlag : ScrollBarBase
    {
        private readonly bool _showButtons;

        const ushort BUTTON_UP = 0x0824;
        const ushort BUTTON_DOWN = 0x0825;
        const ushort BUTTON_FLAG = 0x0828;

        public ScrollFlag(int x, int y, int height, bool showbuttons) : this()
        {
            X = x;
            Y = y;
            Height = height;

            //TODO:
            _showButtons = false; // showbuttons;
        }

        public ScrollFlag()
        {
            AcceptMouseInput = true;

            ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);

            if (gumpInfoFlag.Texture == null)
            {
                Dispose();

                return;
            }

            ref readonly var gumpInfoUp = ref Client.Game.UO.Gumps.GetGump(BUTTON_UP);
            ref readonly var gumpInfoDown = ref Client.Game.UO.Gumps.GetGump(BUTTON_DOWN);

            Width = gumpInfoFlag.UV.Width;
            Height = gumpInfoFlag.UV.Height;

            _rectUpButton = new Rectangle(0, 0, gumpInfoUp.UV.Width, gumpInfoUp.UV.Height);
            _rectDownButton = new Rectangle(
                0,
                Height,
                gumpInfoDown.UV.Width,
                gumpInfoDown.UV.Height
            );

            WantUpdateSize = false;
        }

        public override ClickPriority Priority { get; set; } = ClickPriority.High;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            var hueVector = ShaderHueTranslator.GetHueVector(0);

            ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);
            ref readonly var gumpInfoUp = ref Client.Game.UO.Gumps.GetGump(BUTTON_UP);
            ref readonly var gumpInfoDown = ref Client.Game.UO.Gumps.GetGump(BUTTON_DOWN);

            if (MaxValue != MinValue && gumpInfoFlag.Texture != null)
            {
                batcher.Draw(
                    gumpInfoFlag.Texture,
                    new Vector2(x, y + _sliderPosition),
                    gumpInfoFlag.UV,
                    hueVector
                );
            }

            if (_showButtons)
            {
                if (gumpInfoUp.Texture != null)
                {
                    batcher.Draw(gumpInfoUp.Texture, new Vector2(x, y), gumpInfoUp.UV, hueVector);
                }

                if (gumpInfoDown.Texture != null)
                {
                    batcher.Draw(
                        gumpInfoDown.Texture,
                        new Vector2(x, y + Height),
                        gumpInfoDown.UV,
                        hueVector
                    );
                }
            }

            return base.Draw(batcher, x, y);
        }

        protected override int GetScrollableArea()
        {
            ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);

            return Height - gumpInfoFlag.UV.Height;
        }

        protected override void CalculateByPosition(int x, int y)
        {
            if (y != _clickPosition.Y)
            {
                ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);
                int height = gumpInfoFlag.UV.Height;

                y -= (height >> 1);

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

                if (y == 0 && _clickPosition.Y < height >> 1)
                {
                    _clickPosition.Y = height >> 1;
                }
                else if (y == scrollableArea && _clickPosition.Y > Height - (height >> 1))
                {
                    _clickPosition.Y = Height - (height >> 1);
                }

                _value = (int)
                    Math.Round(y / (float)scrollableArea * (MaxValue - MinValue) + MinValue);
            }
        }

        public override bool Contains(int x, int y)
        {
            ref readonly var gumpInfoFlag = ref Client.Game.UO.Gumps.GetGump(BUTTON_FLAG);

            if (gumpInfoFlag.Texture == null)
            {
                return false;
            }

            y -= _sliderPosition;

            return Client.Game.UO.Gumps.PixelCheck(BUTTON_FLAG, x, y);
        }
    }
}
