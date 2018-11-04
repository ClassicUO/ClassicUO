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

using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Input.TextEntry
{
    public class TextEntry : IDisposable
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

            RenderCaret = new RenderedText
            {
                IsUnicode = unicode, Font = font, Hue = hue, Text = "_"
            };
            MaxCharCount = maxcharlength;
            Width = width;
            MaxWidth = maxWidth;
        }

        public int MaxCharCount { get; }

        public int Width { get; }

        public int MaxWidth { get; }

        public bool IsPassword { get; set; }

        public bool NumericOnly { get; set; }

        public ushort Hue
        {
            get => RenderText.Hue;
            set => RenderText.Hue = value;
        }

        public bool IsChanged { get; set; }

        public int Offset { get; set; }

        public Point CaretPosition { get; set; }

        protected int CaretIndex { get; set; }

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

        protected virtual void OnTextChanged()
        {
        }

        protected virtual void OnCaretPositionChanged()
        {
        }

        public void InsertString(string c)
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

                    if (int.Parse(s) > MaxCharCount)
                        return;
                }
                else if (Text.Length >= MaxCharCount) return;
            }

            string text = Text.Insert(CaretIndex, c);
            CaretIndex += c.Length;
            SetText(text);
        }

        public void SetText(string text)
        {
            if (RenderText.MaxWidth > 0)
            {
                int width = RenderText.IsUnicode ? Fonts.GetWidthUnicode(RenderText.Font, text) : Fonts.GetWidthASCII(RenderText.Font, text);
                int len = text.Length;

                while (RenderText.MaxWidth < width && len > 0)
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
                    width = RenderText.IsUnicode ? Fonts.GetWidthUnicode(RenderText.Font, text) : Fonts.GetWidthASCII(RenderText.Font, text);
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
                (x, y) = Fonts.GetCaretPosUnicode(RenderText.Font, RenderText.Text, CaretIndex, Width, RenderText.Align, (ushort) RenderText.FontStyle);
            else
                (x, y) = Fonts.GetCaretPosASCII(RenderText.Font, RenderText.Text, CaretIndex, Width, RenderText.Align, (ushort) RenderText.FontStyle);
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
                CaretIndex = Fonts.CalculateCaretPosUnicode(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort) RenderText.FontStyle);
            else
                CaretIndex = Fonts.CalculateCaretPosASCII(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort) RenderText.FontStyle);

            if (oldPos != CaretIndex)
                UpdateCaretPosition();
        }

        public int GetLinesCount()
        {
            return RenderText.IsUnicode ? Fonts.GetLinesCountUnicode(RenderText.Font, RenderText.Text, RenderText.Align, (ushort) RenderText.FontStyle, Width) : Fonts.GetLinesCountASCII(RenderText.Font, RenderText.Text, RenderText.Align, (ushort) RenderText.FontStyle, Width);
        }

        public void RemoveLineAt(int index)
        {
            int count = GetLinesCount();

            if (count <= index)
                return;
            int current = 0;
            int foundedIndex = 0;

            while (index != count)
            {
                int first = Text.IndexOf('\n', foundedIndex);

                if (first == -1)
                    break;

                if (index == current)
                {
                    Text = Text.Remove(foundedIndex, first - foundedIndex + 1);

                    break;
                }

                foundedIndex = first + 1;
                current++;
            }
        }

        public void Clear()
        {
            Text = string.Empty;
            Offset = 0;
            CaretPosition = Point.Zero;
            CaretIndex = 0;
        }

        public void Dispose()
        {
            RenderText?.Dispose();
            RenderText = null;

            RenderCaret?.Dispose();
            RenderCaret = null;
        }
    }
}