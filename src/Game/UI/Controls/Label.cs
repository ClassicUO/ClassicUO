#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System.Collections.Generic;

using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class Label : Control
    {
        private readonly RenderedText _gText;

        public Label(string text, bool isunicode, ushort hue, int maxwidth = 0, byte font = 0xFF, FontStyle style = FontStyle.None, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT, bool ishtml = false)
        {
            _gText = RenderedText.Create(text, hue, font, isunicode, style, align, maxwidth, isHTML: ishtml);

            AcceptMouseInput = false;
            Width = _gText.Width;
            Height = _gText.Height;
        }

        public Label(List<string> parts, string[] lines) : this(int.TryParse(parts[4], out int lineIndex) && lineIndex >= 0 && lineIndex < lines.Length ? lines[lineIndex] : string.Empty, true, (ushort) (UInt16Converter.Parse(parts[3]) + 1), 0, style: FontStyle.BlackBorder)
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
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
            if (IsDisposed) return false;

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