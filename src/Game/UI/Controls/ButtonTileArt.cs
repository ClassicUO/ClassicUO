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

using System.Collections.Generic;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class ButtonTileArt : Button
    {
        private readonly ushort _hue;
        private readonly bool _isPartial;
        private readonly UOTexture _texture;
        private readonly int _tileX, _tileY;

        public ButtonTileArt(List<string> gparams) : base(gparams)
        {
            X = int.Parse(gparams[1]);
            Y = int.Parse(gparams[2]);
            ushort graphic = UInt16Converter.Parse(gparams[8]);
            _hue = UInt16Converter.Parse(gparams[9]);
            _tileX = int.Parse(gparams[10]);
            _tileY = int.Parse(gparams[11]);
            ContainsByBounds = true;
            IsFromServer = true;
            _texture = ArtLoader.Instance.GetTexture(graphic);

            if (_texture == null)
            {
                Dispose();

                return;
            }

            _isPartial = TileDataLoader.Instance.StaticData[graphic].IsPartialHue;
        }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (_texture != null)
            {
                _texture.Ticks = Time.Ticks;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            base.Draw(batcher, x, y);

            ResetHueVector();

            ShaderHueTranslator.GetHueVector(ref HueVector, _hue, _isPartial, 0);

            return batcher.Draw2D(_texture, x + _tileX, y + _tileY, ref HueVector);
        }
    }
}