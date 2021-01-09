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
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class ColorBox : Control
    {
        private Color _colorRGBA;
        private Texture2D _texture;

        public ColorBox(int width, int height, ushort hue, uint pol)
        {
            CanMove = false;

            SetColor(hue, pol);

            Width = width;
            Height = height;

            WantUpdateSize = false;
        }

        public ushort Hue { get; private set; }

        public void SetColor(ushort hue, uint pol)
        {
            Hue = hue;

            (byte b, byte g, byte r, byte a) = HuesHelper.GetBGRA(HuesHelper.RgbaToArgb(pol));

            _colorRGBA = new Color(a, b, g, r);

            if (_colorRGBA.A == 0)
            {
                _colorRGBA.A = 0xFF;
            }

            if (_texture == null || _texture.IsDisposed)
            {
                _texture = new UOTexture(1, 1);
            }

            _texture.SetData(new Color[1] { _colorRGBA });
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            return batcher.Draw2D
            (
                _texture,
                x,
                y,
                Width,
                Height,
                ref HueVector
            );
        }

        public override void Dispose()
        {
            _texture?.Dispose();
            base.Dispose();
        }
    }
}