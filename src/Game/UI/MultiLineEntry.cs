#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System;

using ClassicUO.IO;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI
{
    internal class MultiLineEntry : AbstractEntry
    {
        public MultiLineEntry(byte font, int maxcharlength = -1, int maxWidth = 0, int width = 0, bool unicode = true, FontStyle style = FontStyle.None, ushort hue = 0xFFFF) : base(maxcharlength, width, maxWidth)
        {
            RenderText = RenderedText.Create(string.Empty, hue, font, unicode, style, maxWidth: width);
            RenderCaret = RenderedText.Create("_", hue, font, unicode, (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None, maxWidth: width);
            MaxLines = 0;
        }

        public int MaxLines { get; internal set; }

        protected virtual void OnTextChanged()
        {
        }

        public string InsertString(string c)
        {
            if (CaretIndex < 0)
                CaretIndex = 0;

            if (CaretIndex > Text.Length)
                CaretIndex = Text.Length;

            if (MaxCharCount > 0)
                if (Text.Length >= MaxCharCount)
                    return c;

            string text = Text.Insert(CaretIndex, c);
            int count = c.Length;

            if (MaxLines > 0)
            {
                var newlines = GetLinesCharsCount(text);

                if (newlines.Length > MaxLines)
                {
                    count = 0;

                    for (int l = newlines.Length - 1; l >= MaxLines; --l)
                        count += newlines[l];
                    c = text;

                    text = text.Remove(Math.Min(Math.Max(0, text.Length - 1), text.Length - count));
                    c = c.Length > text.Length ? c.Substring(text.Length) : null;
                    if (c != null) count -= c.Length;
                }
                else
                    c = null;
            }
            else
                c = null;

            count = CaretIndex += count;
            SetText(text, count);

            return c;
        }

        public void SetText(string text, int newcaretpos)
        {
            if (MaxCharCount > 0)
            {
                if (text.Length >= MaxCharCount)
                    text = text.Remove(MaxCharCount - 1);
            }

            if (MaxWidth > 0)
            {
                int width = RenderText.IsUnicode ? FileManager.Fonts.GetWidthUnicode(RenderText.Font, text) : FileManager.Fonts.GetWidthASCII(RenderText.Font, text);
                int len = text.Length;

                while (MaxWidth < width && len > 0)
                {
                    if (CaretIndex > 0)
                    {
                        if (CaretIndex < 1)
                            return;

                        CaretIndex--;
                    }

                    text = CaretIndex < text.Length ? text.Remove(CaretIndex, 1) : text.Remove(text.Length - 1);
                    len--;
                    width = RenderText.IsUnicode ? FileManager.Fonts.GetWidthUnicode(RenderText.Font, text) : FileManager.Fonts.GetWidthASCII(RenderText.Font, text);
                }
            }

            CaretIndex = Math.Min(text.Length, newcaretpos);
            Text = text;
        }

        public int[] GetLinesCharsCount()
        {
            return RenderText.IsUnicode ? FileManager.Fonts.GetLinesCharsCountUnicode(RenderText.Font, RenderText.Text, RenderText.Align, (ushort) RenderText.FontStyle, Width, true) : FileManager.Fonts.GetLinesCharsCountASCII(RenderText.Font, RenderText.Text, RenderText.Align, (ushort) RenderText.FontStyle, Width, true);
        }

        public int[] GetLinesCharsCount(string text)
        {
            return RenderText.IsUnicode ? FileManager.Fonts.GetLinesCharsCountUnicode(RenderText.Font, text, RenderText.Align, (ushort) RenderText.FontStyle, Width, true, true) : FileManager.Fonts.GetLinesCharsCountASCII(RenderText.Font, text, RenderText.Align, (ushort) RenderText.FontStyle, Width, true, true);
        }
    }
}