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

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI
{
    internal class TextEntry : AbstractEntry
    {
        private string _plainText;

        public TextEntry(byte font, int maxcharlength = -1, int maxWidth = 0, int width = 0, bool unicode = true, FontStyle style = FontStyle.None, ushort hue = 0xFFFF) : base(maxcharlength, width, maxWidth)
        {
            RenderText = new RenderedText
            {
                IsUnicode = unicode,
                Font = font,
                MaxWidth = width,
                FontStyle = style,
                Hue = hue,
                Text = string.Empty
            };

            if (maxWidth > 0)
                RenderText.FontStyle |= FontStyle.Cropped;

            RenderCaret = new RenderedText
            {
                IsUnicode = unicode,
                Font = font,
                Hue = hue,
                FontStyle = (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None,
                Text = "_"
            };
        }

        public bool IsPassword { get; set; }

        public bool NumericOnly { get; set; }

        public bool SafeCharactersOnly { get; set; }

        public ushort Hue
        {
            get => RenderText.Hue;
            set
            {
                if (RenderText.Hue != value)
                {
                    RenderCaret.Hue = RenderText.Hue = value;
                    RenderText.CreateTexture();
                    RenderCaret.CreateTexture();
                }
            } 
        }

        public override string Text
        {
            get => IsPassword ? _plainText : RenderText.Text;
            set
            {
                _plainText = value;
                RenderText.Text = IsPassword ? new string('*', value.Length) : value;
                IsChanged = true;
            }
        }

        public void InsertString(string c)
        {
            if (CaretIndex < 0)
                CaretIndex = 0;

            if (CaretIndex > Text.Length)
                CaretIndex = Text.Length;

            if (MaxCharCount > 0)
            {
                if (SafeCharactersOnly)
                {
                    if ((int)Convert.ToChar(c) < 32 || (int)Convert.ToChar(c) > 126)
                        return;
                }
                else if (NumericOnly)
                {
                    string s = Text;
                    s = s.Insert(CaretIndex, c);

                    if (!int.TryParse(s, out int value) || value >= MaxCharCount)
                        return;
                }
                else if (Text.Length >= MaxCharCount)
                {
                    return;
                }
            }

            string text = Text.Insert(CaretIndex, c);
            int count = CaretIndex + c.Length;
            SetText(text);
            CaretIndex = Math.Min(count, text.Length);
        }

        public void SetText(string text)
        {
            if (MaxCharCount > 0)
            {
                if (SafeCharactersOnly)
                {
                    char[] ch = text.ToCharArray();
                    string safeString = "";
                    foreach (char c in ch)
                    {
                        if ((int)Convert.ToChar(c) >= 32 && (int)Convert.ToChar(c) <= 126)
                            safeString += c;
                    }
                    if (safeString.Length >= MaxCharCount)
                        text = safeString.Substring(0, MaxCharCount);
                    else
                        text = safeString;
                }
                else if (NumericOnly)
                {
                    string str = text;
                    while (true)
                    {
                        int len = str.Length;

                        if (int.TryParse(str, out int result) && result >= MaxCharCount && len > 0)
                            str = str.Substring(len - 1);
                        else 
                            break;
                    }
                }
                else if (text.Length >= MaxCharCount)
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

                    if (CaretIndex < text.Length)
                        text = text.Remove(CaretIndex, 1);
                    else
                        text = text.Remove(text.Length - 1);
                    len--;
                    width = RenderText.IsUnicode ? FileManager.Fonts.GetWidthUnicode(RenderText.Font, text) : FileManager.Fonts.GetWidthASCII(RenderText.Font, text);
                }
            }
            Text = text;
        }
    }
}