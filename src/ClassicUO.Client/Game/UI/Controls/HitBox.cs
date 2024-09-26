#region license

// Copyright (c) 2024, andreakarasho
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

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    public class HitBox : Control
    {
        public HitBox
        (
            int x,
            int y,
            int w,
            int h,
            string tooltip = null,
            float alpha = 0.25f
        )
        {
            CanMove = false;
            AcceptMouseInput = true;
            Alpha = alpha;
            _texture = SolidColorTextureCache.GetTexture(Color.White);

            X = x;
            Y = y;
            Width = w;
            Height = h;
            WantUpdateSize = false;

            SetTooltip(tooltip);
        }


        public override ClickPriority Priority { get; set; } = ClickPriority.High;
        protected readonly Texture2D _texture;


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            if (MouseIsOver)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector
                                    (
                                        0,
                                        false,
                                        Alpha,
                                        true
                                    );

                batcher.Draw
                (
                    _texture,
                    new Vector2(x, y),
                    new Rectangle(0, 0, Width, Height),
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }
    }
}