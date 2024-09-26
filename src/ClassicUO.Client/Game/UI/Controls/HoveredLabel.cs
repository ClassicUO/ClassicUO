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

using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class HoveredLabel : Label
    {
        private readonly ushort _overHue, _normalHue, _selectedHue;
        private Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

        public HoveredLabel
        (
            string text,
            bool isunicode,
            ushort hue,
            ushort overHue,
            ushort selectedHue,
            int maxwidth = 0,
            byte font = 255,
            FontStyle style = FontStyle.None,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT
        ) : base
        (
            $" {text}",
            isunicode,
            hue,
            maxwidth,
            font,
            style,
            align
        )
        {
            _overHue = overHue;
            _normalHue = hue;
            _selectedHue = selectedHue;
            AcceptMouseInput = true;
        }

        public bool DrawBackgroundCurrentIndex;
        public bool IsSelected, ForceHover;

        public override void Update()
        {
            if (IsSelected)
            {
                if (Hue != _selectedHue)
                {
                    Hue = _selectedHue;
                }
            }
            else if (MouseIsOver || ForceHover)
            {
                if (Hue != _overHue)
                {
                    Hue = _overHue;
                }
            }
            else if (Hue != _normalHue)
            {
                Hue = _normalHue;
            }


            base.Update();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (DrawBackgroundCurrentIndex && MouseIsOver && !string.IsNullOrWhiteSpace(Text))
            {
                batcher.Draw
                (
                    SolidColorTextureCache.GetTexture(Color.Gray),
                    new Rectangle
                    (
                        x,
                        y + 2,
                        Width - 4,
                        Height - 4
                    ),
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }
    }
}