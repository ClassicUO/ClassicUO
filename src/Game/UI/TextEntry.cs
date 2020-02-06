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

using System;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI
{
    internal class TextEntry : AbstractEntry
    {
        private string _plainText;

        public TextEntry(byte font, int maxcharlength = -1, int maxWidth = 0, int width = 0, bool unicode = true, FontStyle style = FontStyle.None, ushort hue = 0xFFFF, TEXT_ALIGN_TYPE align = 0) : base(maxcharlength, width, maxWidth)
        {
            RenderText = RenderedText.Create(String.Empty, hue, font, unicode, style, maxWidth: width, align: align);

            if (maxWidth > 0)
                RenderText.FontStyle |= FontStyle.Cropped;

            RenderCaret = RenderedText.Create("_", hue, font, unicode, (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None, align: align);
        }

        public bool IsPassword { get; set; }

        public uint ValidationRules { get; set; }

        public bool SafeCharactersOnly
        {
            set
            {
                if (value)
                    ValidationRules = (uint) (TEXT_ENTRY_RULES.NUMERIC | TEXT_ENTRY_RULES.SYMBOL | TEXT_ENTRY_RULES.SPACE | TEXT_ENTRY_RULES.LETTER);
                else
                    ValidationRules = ValidationRules - (uint) (TEXT_ENTRY_RULES.NUMERIC | TEXT_ENTRY_RULES.SYMBOL | TEXT_ENTRY_RULES.SPACE | TEXT_ENTRY_RULES.LETTER);
            }
        }

        public bool NumericOnly
        {
            set
            {
                if (value)
                    ValidationRules = (uint) TEXT_ENTRY_RULES.NUMERIC;
                else
                    ValidationRules = ValidationRules - (uint) TEXT_ENTRY_RULES.NUMERIC;
            }
        }

        public bool UNumericOnly
        {
            set
            {
                if (value)
                    ValidationRules = (uint) TEXT_ENTRY_RULES.NUMERIC + (uint) TEXT_ENTRY_RULES.UNUMERIC;
                else
                    ValidationRules = ValidationRules - (uint) TEXT_ENTRY_RULES.UNUMERIC;
            }
        }

        public bool LettersOnly
        {
            set
            {
                if (value)
                    ValidationRules = (uint) TEXT_ENTRY_RULES.LETTER;
                else
                    ValidationRules = ValidationRules - (uint) TEXT_ENTRY_RULES.LETTER;
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
                    bool allowChar = (ValidationRules & (uint) TEXT_ENTRY_RULES.SYMBOL) != 0 && (c1 >= 33 && c1 <= 47 || c1 >= 58 && c1 <= 64 || c1 >= 91 && c1 <= 96 || c1 >= 123 && c1 <= 126);

                    if ((ValidationRules & (uint) TEXT_ENTRY_RULES.NUMERIC) != 0 && (c1 >= 48 && c1 <= 57 || (ValidationRules & (uint) TEXT_ENTRY_RULES.UNUMERIC) == 0 && Text.Length == 0 && c1 == 45))
                        allowChar = true;

                    if ((ValidationRules & (uint) TEXT_ENTRY_RULES.LETTER) != 0 && (c1 >= 65 && c1 <= 90 || c1 >= 97 && c1 <= 122 || c1 == 39))
                        allowChar = true;

                    if ((ValidationRules & (uint) TEXT_ENTRY_RULES.SPACE) != 0 && c1 == 32)
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

                        if ((ValidationRules & (uint) TEXT_ENTRY_RULES.SYMBOL) != 0 && (c1 >= 33 && c1 <= 47 || c1 >= 58 && c1 <= 64 || c1 >= 91 && c1 <= 96 || c1 >= 123 && c1 <= 126))
                            allowChar = true;

                        if ((ValidationRules & (uint) TEXT_ENTRY_RULES.NUMERIC) != 0 && (c1 >= 48 && c1 <= 57 || c1 == 45))
                            allowChar = true;

                        if ((ValidationRules & (uint) TEXT_ENTRY_RULES.LETTER) != 0 && (c1 >= 65 && c1 <= 90 || c1 >= 97 && c1 <= 122 || c1 == 39))
                            allowChar = true;

                        if ((ValidationRules & (uint) TEXT_ENTRY_RULES.SPACE) != 0 && c1 == 32)
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
                int width = RenderText.IsUnicode ? 
                    FontsLoader.Instance.GetWidthUnicode(RenderText.Font, text) : 
                    FontsLoader.Instance.GetWidthASCII(RenderText.Font, text);
                int len = text.Length;

                while (MaxWidth < width && len > 0)
                {
                    if (CaretIndex > 0)
                    {
                        CaretIndex--;
                    }

                    text = CaretIndex < text.Length ? text.Remove(CaretIndex, 1) : text.Remove(text.Length - 1);
                    len--;
                    width = RenderText.IsUnicode ? FontsLoader.Instance.GetWidthUnicode(RenderText.Font, text) : FontsLoader.Instance.GetWidthASCII(RenderText.Font, text);
                }
            }

            Text = text;
        }
    }
}