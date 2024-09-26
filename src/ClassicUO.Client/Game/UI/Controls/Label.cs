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

using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    public class Label : Control
    {
        private readonly RenderedText _gText;

        public Label
        (
            string text,
            bool isunicode,
            ushort hue,
            int maxwidth = 0,
            byte font = 0xFF,
            FontStyle style = FontStyle.None,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT,
            bool ishtml = false
        )
        {
            _gText = RenderedText.Create
            (
                text,
                hue,
                font,
                isunicode,
                style,
                align,
                maxwidth,
                isHTML: ishtml
            );

            AcceptMouseInput = false;
            Width = _gText.Width;
            Height = _gText.Height;
        }

        public Label(List<string> parts, string[] lines) : this
        (
            int.TryParse(parts[4], out int lineIndex) && lineIndex >= 0 && lineIndex < lines.Length ? lines[lineIndex] : string.Empty,
            true,
            (ushort) (UInt16Converter.Parse(parts[3]) + 1),
            0,
            style: FontStyle.BlackBorder
        )
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            IsFromServer = true;
        }

        public string Text
        {
            get => _gText.Text;
            set
            {
                _gText.Text = value;
                Width = _gText.Width;
                Height = _gText.Height;
            }
        }


        public ushort Hue
        {
            get => _gText.Hue;
            set
            {
                if (_gText.Hue != value)
                {
                    _gText.Hue = value;
                    _gText.CreateTexture();
                }
            }
        }


        public byte Font => _gText.Font;

        public bool Unicode => _gText.IsUnicode;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            _gText.Draw(batcher, x, y, Alpha);

            return base.Draw(batcher, x, y);
        }

        public override void Dispose()
        {
            base.Dispose();
            _gText.Destroy();
        }
    }
}