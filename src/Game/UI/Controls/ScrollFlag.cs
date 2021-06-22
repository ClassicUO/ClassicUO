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

            UOTexture texture_flag = GumpsLoader.Instance.GetTexture(0x0828);

            if (texture_flag == null)
            {
                Dispose();

                return;
            }

            Width = texture_flag.Width;
            Height = texture_flag.Height;

            UOTexture texture_button_up = GumpsLoader.Instance.GetTexture(0x0824);
            UOTexture texture_button_down = GumpsLoader.Instance.GetTexture(0x0825);

            _rectUpButton = new Rectangle(0, 0, texture_button_up.Width, texture_button_up.Height);
            _rectDownButton = new Rectangle(0, Height, texture_button_down.Width, texture_button_down.Height);

            WantUpdateSize = false;
        }

        public override ClickPriority Priority { get; set; } = ClickPriority.High;


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            UOTexture texture_flag = GumpsLoader.Instance.GetTexture(0x0828);
            UOTexture texture_button_up = GumpsLoader.Instance.GetTexture(0x0824);
            UOTexture texture_button_down = GumpsLoader.Instance.GetTexture(0x0825);


            if (MaxValue != MinValue && texture_flag != null)
            {
                batcher.Draw2D(texture_flag, x, (int) (y + _sliderPosition), ref HueVector);
            }

            if (_showButtons)
            {
                if (texture_button_up != null)
                {
                    batcher.Draw2D(texture_button_up, x, y, ref HueVector);
                }

                if (texture_button_down != null)
                {
                    batcher.Draw2D(texture_button_down, x, y + Height, ref HueVector);
                }
            }

            return base.Draw(batcher, x, y);
        }

        protected override int GetScrollableArea()
        {
            UOTexture texture = GumpsLoader.Instance.GetTexture(0x0828);

            return Height - texture?.Height ?? 0;
        }


        protected override void CalculateByPosition(int x, int y)
        {
            if (y != _clickPosition.Y)
            {
                UOTexture texture = GumpsLoader.Instance.GetTexture(0x0828);
                int height = texture?.Height ?? 0;

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
            UOTexture texture_flag = GumpsLoader.Instance.GetTexture(0x0828);

            if (texture_flag == null)
            {
                return false;
            }

            y -= _sliderPosition;

            return GumpsLoader.Instance.PixelCheck(0x0828, x, y);
        }
    }
}