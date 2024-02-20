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

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    public class ClickableColorBox : ColorBox
    {
        private readonly bool useModernSelector;

        public ClickableColorBox
        (
            int x,
            int y,
            int w,
            int h,
            ushort hue,
            bool useModernSelector = false
        ) : base(w, h, hue)
        {
            X = x;
            Y = y;
            WantUpdateSize = false;

            GumpPic background = new GumpPic(0, 0, 0x00D4, 0);
            Add(background);

            Width = background.Width;
            Height = background.Height;
            this.useModernSelector = useModernSelector;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Children.Count != 0)
            {
                Children[0].Draw(batcher, x, y);
            }

            batcher.Draw
            (
               SolidColorTextureCache.GetTexture(Color.White),
               new Rectangle
               (
                   x + 3,
                   y + 3,
                   Width - 6,
                   Height - 6
                ),
                hueVector
            );

            return true;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                UIManager.GetGump<ColorPickerGump>()?.Dispose();
                if (useModernSelector)
                {
                    UIManager.Add(new ModernColorPicker(s => Hue = s) { X = 100, Y = 100 });
                }
                else
                {
                    ColorPickerGump pickerGump = new ColorPickerGump
                    (
                        0,
                        0,
                        100,
                        100,
                        s => Hue = s
                    );

                    UIManager.Add(pickerGump);
                }
            }
        }
    }
}