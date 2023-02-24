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
          
            if (GumpsLoader.Instance.GetGumpTexture(BUTTON_FLAG, out var boundsFlag) == null)
            {
                Dispose();

                return;
            }

            _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_UP, out var boundsButtonUp);
            _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_DOWN, out var boundsButtonDown);

            Width = boundsFlag.Width;
            Height = boundsFlag.Height;

            _rectUpButton = new Rectangle(0, 0, boundsButtonUp.Width, boundsButtonUp.Height);
            _rectDownButton = new Rectangle(0, Height, boundsButtonDown.Width, boundsButtonDown.Height);

            WantUpdateSize = false;
        }

        public override ClickPriority Priority { get; set; } = ClickPriority.High;


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            var textureFlag = GumpsLoader.Instance.GetGumpTexture(BUTTON_FLAG, out var boundsFlag);
            var textureButtonUp = GumpsLoader.Instance.GetGumpTexture(BUTTON_UP, out var boundsButtonUp);
            var textureButtonDown = GumpsLoader.Instance.GetGumpTexture(BUTTON_DOWN, out var boundsButtonDown);


            if (MaxValue != MinValue && textureFlag != null)
            {
                batcher.Draw
                (
                    textureFlag,
                    new Vector2(x,  y + _sliderPosition),
                    boundsFlag,
                    hueVector
                );
            }

            if (_showButtons)
            {
                if (textureButtonUp != null)
                {
                    batcher.Draw
                    (
                        textureButtonUp,
                        new Vector2(x, y),
                        boundsButtonUp,
                        hueVector
                    );
                }

                if (textureButtonDown != null)
                {
                    batcher.Draw
                    (
                        textureButtonDown,
                        new Vector2(x, y + Height),
                        boundsButtonDown,
                        hueVector
                    );
                }
            }

            return base.Draw(batcher, x, y);
        }

        protected override int GetScrollableArea()
        {
            _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_FLAG, out var boundsFlag);

            return Height - boundsFlag.Height;
        }


        protected override void CalculateByPosition(int x, int y)
        {
            if (y != _clickPosition.Y)
            {
                _ = GumpsLoader.Instance.GetGumpTexture(BUTTON_FLAG, out var boundsFlag);
                int height = boundsFlag.Height;

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

                _value = (int) Math.Round(y / (float) scrollableArea * (MaxValue - MinValue) + MinValue);
            }
        }


        public override bool Contains(int x, int y)
        {
            if (GumpsLoader.Instance.GetGumpTexture(BUTTON_FLAG, out _) == null)
            {
                return false;
            }

            y -= _sliderPosition;

            return GumpsLoader.Instance.PixelCheck(BUTTON_FLAG, x, y);
        }
    }
}