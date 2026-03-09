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
//    documentation and/or other materials distributed with the distribution.
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
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
using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    public static class UOLabelHue
    {
        public const ushort Text = 0x0481;
        public const ushort Accent = 32;
        public const ushort Hover = 35;
    }

    public class UOLabel : Control
    {
        private readonly RenderedText _renderedText;

        public UOLabel
        (
            string text,
            byte font = 0xFF,
            ushort hue = 0xFFFF,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT,
            int maxWidth = 0,
            FontStyle style = FontStyle.BlackBorder,
            bool isUnicode = true,
            bool isHtml = false
        )
        {
            _renderedText = RenderedText.Create
            (
                text,
                hue,
                font,
                isUnicode,
                style,
                align,
                maxWidth,
                isHTML: isHtml
            );
            AcceptMouseInput = false;
            Width = _renderedText.Width;
            Height = _renderedText.Height;
        }

        public string Text
        {
            get => _renderedText.Text;
            set
            {
                _renderedText.Text = value;
                Width = _renderedText.Width;
                Height = _renderedText.Height;
            }
        }

        public ushort Hue
        {
            get => _renderedText.Hue;
            set
            {
                if (_renderedText.Hue != value)
                {
                    _renderedText.Hue = value;
                    _renderedText.CreateTexture();
                }
            }
        }

        public override bool Contains(int x, int y)
        {
            if (!AcceptMouseInput || IsDisposed)
            {
                return false;
            }
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            _renderedText.Draw(batcher, x, y, Alpha);
            return base.Draw(batcher, x, y);
        }

        public override void Dispose()
        {
            base.Dispose();
            _renderedText?.Destroy();
        }
    }
}
