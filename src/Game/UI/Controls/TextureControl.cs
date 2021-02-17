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

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class TextureControl : Control
    {
        public TextureControl()
        {
            CanMove = true;
            AcceptMouseInput = true;
            ScaleTexture = true;
        }

        public bool ScaleTexture { get; set; }

        public ushort Hue { get; set; }
        public bool IsPartial { get; set; }
        public UOTexture Texture { get; set; }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (Texture != null)
            {
                Texture.Ticks = Time.Ticks;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Texture == null)
            {
                return false;
            }

            ResetHueVector();
            ShaderHueTranslator.GetHueVector(ref HueVector, Hue, IsPartial, Alpha);

            if (ScaleTexture)
            {
                if (Texture is ArtTexture artTexture)
                {
                    int w = Width;
                    int h = Height;
                    Rectangle r = artTexture.ImageRectangle;

                    if (r.Width < Width)
                    {
                        w = r.Width;
                        x += (Width >> 1) - (w >> 1);
                    }

                    if (r.Height < Height)
                    {
                        h = r.Height;
                        y += (Height >> 1) - (h >> 1);
                    }

                    return batcher.Draw2D
                    (
                        Texture,
                        x,
                        y,
                        w,
                        h,
                        r.X,
                        r.Y,
                        r.Width,
                        r.Height,
                        ref HueVector
                    );
                }

                return batcher.Draw2D
                (
                    Texture,
                    x,
                    y,
                    Width,
                    Height,
                    0,
                    0,
                    Texture.Width,
                    Texture.Height,
                    ref HueVector
                );
            }

            return batcher.Draw2D(Texture, x, y, ref HueVector);
        }

        public override void Dispose()
        {
            Texture = null;
            base.Dispose();
        }
    }
}