#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
    internal class TextEntry : IDisposable
    {
        private string _plainText;

        public TextEntry(byte font, int maxcharlength = -1, int maxWidth = 0, int width = 0, bool unicode = true, FontStyle style = FontStyle.None, ushort hue = 0xFFFF)
        {
            RenderText = new RenderedText
            {
                IsUnicode = unicode,
                Font = font,
                MaxWidth = width,
                FontStyle = style,
                Hue = hue
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
            MaxCharCount =/* maxcharlength <= 0 ? 200 :*/ maxcharlength;
            Width = width;
            MaxWidth = maxWidth;
            MaxLines = 0;
        }

        public int MaxCharCount { get; }

        public int Width { get; }
        public int Height => RenderText.Height < 20 ? 20 : RenderText.Height;

        public int MaxWidth { get; }

        public bool IsPassword { get; set; }

        public bool NumericOnly { get; set; }

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

        public bool IsChanged { get; private set; }

        public int Offset { get; set; }

        public Point CaretPosition { get; set; }

        public int CaretIndex { get; protected set; }

        public RenderedText RenderText { get; private set; }

        public RenderedText RenderCaret { get; private set; }

        public string Text
        {
            get => IsPassword ? _plainText : RenderText.Text;
            set
            {
                _plainText = value;
                RenderText.Text = IsPassword ? new string('*', value.Length) : value;
                IsChanged = true;
            }
        }

        public int MaxLines { get; internal set; }

        public void Dispose()
        {
            RenderText?.Dispose();
            RenderText = null;
            RenderCaret?.Dispose();
            RenderCaret = null;
        }

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
            {
                if (NumericOnly)
                {
                    string s = Text;
                    s = s.Insert(CaretIndex, c);

                    if (!int.TryParse(s, out int value) || value >= MaxCharCount)
                        return c;
                }
                else if (Text.Length >= MaxCharCount)
                {
                    return c;
                }
            }

            string text = Text.Insert(CaretIndex, c);
            int count = c.Length;
            if (MaxLines > 0)
            {
                var newlines = GetLinesCharsCount(text);
                if (newlines.Length > MaxLines)
                {
                    count = newlines.Length - MaxLines;
                    for (int i = newlines.Length - 1; i >= MaxLines; --i)
                        count += newlines[i];
                    c = text;
                    text = text.Remove(text.Length - count);
                    c = c.Substring(Math.Min(c.Length - 1, text.Length + 1));
                    count -= c.Length - 1;
                }
                else
                    c = null;
            }
            else
                c = null;

            count = CaretIndex += count;
            SetText(text);
            CaretIndex = count;
            return c;
        }

        public void SetText(string text)
        {
            if (MaxCharCount > 0)
            {
                if (NumericOnly)
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
            CaretIndex = text.Length;
            Text = text;
        }

        public void RemoveChar(bool fromleft)
        {
            if (fromleft)
            {
                if (CaretIndex < 1)
                    return;
                CaretIndex--;
            }
            else
            {
                if (CaretIndex >= Text.Length)
                    return;
            }

            if (CaretIndex < Text.Length)
                Text = Text.Remove(CaretIndex, 1);
            else if (CaretIndex > Text.Length)
                Text = Text.Remove(Text.Length - 1);
        }

        public void SeekCaretPosition(int value)
        {
            CaretIndex += value;

            if (CaretIndex < 0)
                CaretIndex = 0;

            if (CaretIndex > Text.Length)
                CaretIndex = Text.Length;
            IsChanged = true;
        }

        public void SetCaretPosition(int value)
        {
            CaretIndex = value;

            if (CaretIndex < 0)
                CaretIndex = 0;

            if (CaretIndex > Text.Length)
                CaretIndex = Text.Length;
            IsChanged = true;
        }

        public void UpdateCaretPosition()
        {
            int x, y;

            if (RenderText.IsUnicode)
                (x, y) = FileManager.Fonts.GetCaretPosUnicode(RenderText.Font, RenderText.Text, CaretIndex, Width, RenderText.Align, (ushort) RenderText.FontStyle);
            else
                (x, y) = FileManager.Fonts.GetCaretPosASCII(RenderText.Font, RenderText.Text, CaretIndex, Width, RenderText.Align, (ushort) RenderText.FontStyle);
            CaretPosition = new Point(x, y);

            if (Offset > 0)
            {
                if (CaretPosition.X + Offset < 0)
                    Offset = -CaretPosition.X;
                else if (Width + -Offset < CaretPosition.X)
                    Offset = Width - CaretPosition.X;
            }
            else if (Width + Offset < CaretPosition.X)
                Offset = Width - CaretPosition.X;
            else
                Offset = 0;

            if (IsChanged)
                IsChanged = false;
        }

        public void OnMouseClick(int x, int y)
        {
            int oldPos = CaretIndex;

            if (RenderText.IsUnicode)
                CaretIndex = FileManager.Fonts.CalculateCaretPosUnicode(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort) RenderText.FontStyle);
            else
                CaretIndex = FileManager.Fonts.CalculateCaretPosASCII(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort) RenderText.FontStyle);

            if (oldPos != CaretIndex)
                UpdateCaretPosition();
        }

        public int[] GetLinesCharsCount()
        {
            return RenderText.IsUnicode ? FileManager.Fonts.GetLinesCharsCountUnicode(RenderText.Font, RenderText.Text, RenderText.Align, (ushort) RenderText.FontStyle, Width) : FileManager.Fonts.GetLinesCharsCountASCII(RenderText.Font, RenderText.Text, RenderText.Align, (ushort) RenderText.FontStyle, Width);
        }
        public int[] GetLinesCharsCount(string text)
        {
            return RenderText.IsUnicode ? FileManager.Fonts.GetLinesCharsCountUnicode( RenderText.Font, text, RenderText.Align, (ushort)RenderText.FontStyle, Width ) : FileManager.Fonts.GetLinesCharsCountASCII( RenderText.Font, text, RenderText.Align, (ushort)RenderText.FontStyle, Width );
        }
        public void Clear()
        {
            Text = string.Empty;
            Offset = 0;
            CaretPosition = Point.Zero;
            CaretIndex = 0;
        }
    }
}