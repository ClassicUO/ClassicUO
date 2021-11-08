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
using System.Collections.Generic;
using ClassicUO.Data;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class Label : Control
    {
        private string _text;
        private FontSettings _fontSettings;
        private float _maxWidth;
        private readonly TEXT_ALIGN_TYPE _align;
        private Vector2 _textSize;

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
            _fontSettings.FontIndex = font == 0xFF ? (byte)(Client.Version >= ClientVersion.CV_305D ? 1 : 0) : font;
            _fontSettings.IsUnicode = isunicode;

            _fontSettings.Border = (style & FontStyle.BlackBorder) != 0;
            _fontSettings.Italic = (style & FontStyle.Italic) != 0;
            _fontSettings.Bold = (style & FontStyle.Solid) != 0;
            _fontSettings.Underline = (style & FontStyle.Underline) != 0;

            _maxWidth = maxwidth;
            _align = align;

            if (hue == 0xFFFF)
            {
                hue = 1;
            }

            Hue = (ushort)(hue - 1);
            Text = text;

            AcceptMouseInput = false;
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
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;

                    _textSize = UOFontRenderer.Shared.MeasureString(_text.AsSpan(), _fontSettings, 1f, _maxWidth);
                    Width = (int) Math.Max(_textSize.X, _maxWidth);
                    Height = (int)_textSize.Y;
                }
            }
        }

        public ushort Hue { get; set; }
        public byte Font => _fontSettings.FontIndex;
        public bool Unicode => _fontSettings.IsUnicode;



        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            Vector2 pos = new Vector2(x, y);

            if (_align == TEXT_ALIGN_TYPE.TS_CENTER)
            {
                pos.X = x + (_maxWidth * 0.5f) - (_textSize.X * 0.5f);
            }

            Vector3 hueVec = new Vector3(Hue, 0f, Alpha);
            if (Hue != 0)
            {
                hueVec.Y = 1f;
            }

            UOFontRenderer.Shared.Draw
            (
                batcher,
                Text.AsSpan(),
                pos,
                1f,
                _fontSettings,
                hueVec,
                false,
                _maxWidth
            );

            return base.Draw(batcher, x, y);
        }
    }
}