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
    internal class TextEntry : AbstractEntry
    {
        private string _plainText;

        public TextEntry(byte font, int maxcharlength = -1, int maxWidth = 0, int width = 0, bool unicode = true, FontStyle style = FontStyle.None, ushort hue = 0xFFFF) : base(maxcharlength, width, maxWidth)
        {
            RenderText = RenderedText.Create(String.Empty, hue, font, unicode, style, maxWidth: width);

            if (maxWidth > 0)
                RenderText.FontStyle |= FontStyle.Cropped;

            RenderCaret = RenderedText.Create("_", hue, font, unicode, (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None);
        }

        public bool IsPassword { get; set; }

        public uint ValidationRules { get; set; }

        public bool SafeCharactersOnly
        {
            set
            {
                if (value)
                    ValidationRules = (uint) (Constants.RULES.NUMERIC | Constants.RULES.SYMBOL | Constants.RULES.SPACE | Constants.RULES.LETTER);
                else
                    ValidationRules = ValidationRules - (uint) (Constants.RULES.NUMERIC | Constants.RULES.SYMBOL | Constants.RULES.SPACE | Constants.RULES.LETTER);
            }
        }

        public bool NumericOnly
        {
            set
            {
                if (value)
                    ValidationRules = (uint) Constants.RULES.NUMERIC;
                else
                    ValidationRules = ValidationRules - (uint) Constants.RULES.NUMERIC;
            }
        }

        public bool UNumericOnly
        {
            set
            {
                if (value)
                    ValidationRules = (uint) Constants.RULES.NUMERIC + (uint) Constants.RULES.UNUMERIC;
                else
                    ValidationRules = ValidationRules - (uint) Constants.RULES.UNUMERIC;
            }
        }

        public bool LettersOnly
        {
            set
            {
                if (value)
                    ValidationRules = (uint) Constants.RULES.LETTER;
                else
                    ValidationRules = ValidationRules - (uint) Constants.RULES.LETTER;
            }
        }

        public override string Text
        {
            get => IsPassword ? _plainText : base.Text;
            set
            {
                _plainText = value;
                base.Text = IsPassword ? new string('*', value.Length) : value;
            }
        }

        public void InsertString(string c)
        {
            if (CaretIndex < 0)
                CaretIndex = 0;

            if (CaretIndex > Text.Length)
                CaretIndex = Text.Length;

            if (MaxCharCount > 0 && Text.Length >= MaxCharCount)
                return;

            if (ValidationRules != 0)
            {
                foreach (char c1 in c)
                {
                    bool allowChar = (ValidationRules & (uint) Constants.RULES.SYMBOL) != 0 && (c1 >= 33 && c1 <= 47 || c1 >= 58 && c1 <= 64 || c1 >= 91 && c1 <= 96 || c1 >= 123 && c1 <= 126);

                    if ((ValidationRules & (uint) Constants.RULES.NUMERIC) != 0 && (c1 >= 48 && c1 <= 57 || (ValidationRules & (uint) Constants.RULES.UNUMERIC) == 0 && Text.Length == 0 && c1 == 45))
                        allowChar = true;

                    if ((ValidationRules & (uint) Constants.RULES.LETTER) != 0 && (c1 >= 65 && c1 <= 90 || c1 >= 97 && c1 <= 122 || c1 == 39))
                        allowChar = true;

                    if ((ValidationRules & (uint) Constants.RULES.SPACE) != 0 && c1 == 32)
                        allowChar = true;

                    if (!allowChar)
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
            if (text == null)
                text = string.Empty;

            if (ValidationRules != 0)
            {
                char[] ch = text.ToCharArray();
                string safeString = "";

                if (ch.Length > 0)
                {
                    foreach (char c in ch)
                    {
                        bool allowChar = false;

                        var c1 = (int) Convert.ToChar(c);

                        if ((ValidationRules & (uint) Constants.RULES.SYMBOL) != 0 && (c1 >= 33 && c1 <= 47 || c1 >= 58 && c1 <= 64 || c1 >= 91 && c1 <= 96 || c1 >= 123 && c1 <= 126))
                            allowChar = true;

                        if ((ValidationRules & (uint) Constants.RULES.NUMERIC) != 0 && (c1 >= 48 && c1 <= 57 || c1 == 45))
                            allowChar = true;

                        if ((ValidationRules & (uint) Constants.RULES.LETTER) != 0 && (c1 >= 65 && c1 <= 90 || c1 >= 97 && c1 <= 122 || c1 == 39))
                            allowChar = true;

                        if ((ValidationRules & (uint) Constants.RULES.SPACE) != 0 && c1 == 32)
                            allowChar = true;

                        if (allowChar)
                            safeString += c;
                    }
                }

                if (safeString.Length > MaxCharCount && MaxCharCount > 0)
                    text = safeString.Substring(0, MaxCharCount);
                else
                    text = safeString;
            }

            if (MaxCharCount > 0 && text.Length > MaxCharCount)
                text = text.Remove(MaxCharCount - 1);

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

            Text = text;
        }
    }
}