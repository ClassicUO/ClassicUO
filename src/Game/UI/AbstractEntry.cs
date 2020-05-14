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

using ClassicUO.Configuration;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI
{
    internal abstract class AbstractEntry
    {
        private int? _height;
        private bool _isChanged;

        protected AbstractEntry(int maxcharlength, int width, int maxWidth)
        {
            MaxCharCount = maxcharlength;
            Width = width;
            MaxWidth = maxWidth;
        }

        public RenderedText RenderText { get; protected set; }

        public RenderedText RenderCaret { get; protected set; }

        public Point CaretPosition { get; set; }
        public int CaretIndex { get; protected set; }

        public ushort Hue
        {
            get => RenderCaret.Hue;
            set
            {
                if (RenderCaret.Hue != value)
                {
                    RenderCaret.Hue = value;
                    RenderText.Hue = value;
                    RenderCaret.CreateTexture();
                    RenderText.CreateTexture();
                }
            }
        }

        public virtual string Text
        {
            get => RenderText.Text;
            set
            {
                RenderText.Text = value;
                IsChanged = true;
            }
        }

        public bool IsChanged
        {
            get => _isChanged;
            set
            {
                _isChanged = value;
            }
        }

        public int MaxCharCount { get; }

        public int Width { get; }
        public int Height => _height.HasValue && _height.Value > RenderText.Height ? _height.Value : RenderText.Height < 15 ? 15 : RenderText.Height;

        public int MaxWidth { get; }

        public int Offset { get; set; }

        public void SetHeight(int h)
        {
            _height = h;
            if (h > 0)
                RenderText.MaxHeight = h;
        }

        public void Destroy()
        {
            RenderText?.Destroy();
            RenderCaret?.Destroy();
        }

        public bool RemoveChar(bool fromleft)
        {
            int start = -1, end = CaretIndex;

            if (fromleft)
            {
                if (CaretIndex < 1)
                    return false;

                CaretIndex--;
            }
            else
            {
                if (CaretIndex >= Text.Length)
                    return false;
            }

            if (start > -1)
            {
                Text = Text.Remove(start, end - start);
            }
            else
            {
                if (CaretIndex >= 0 && Text.Length > 0)
                {
                    if (CaretIndex < Text.Length)
                        Text = Text.Remove(CaretIndex, 1);
                    else if (CaretIndex > Text.Length)
                        Text = Text.Remove(Text.Length - 1);
                }
            }
            return true;
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
                (x, y) = FontsLoader.Instance.GetCaretPosUnicode(RenderText.Font, RenderText.Text, CaretIndex, Width, RenderText.Align, (ushort) RenderText.FontStyle);
            else
                (x, y) = FontsLoader.Instance.GetCaretPosASCII(RenderText.Font, RenderText.Text, CaretIndex, Width, RenderText.Align, (ushort) RenderText.FontStyle);
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

      
        public void OnMouseClick(int x, int y, bool mouseclick = true)
        {
            int oldPos = CaretIndex;

            CaretIndex = RenderText.IsUnicode ? FontsLoader.Instance.CalculateCaretPosUnicode(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort) RenderText.FontStyle) : FontsLoader.Instance.CalculateCaretPosASCII(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort) RenderText.FontStyle);

            if (oldPos != CaretIndex)
                UpdateCaretPosition();
        }

        internal void OnSelectionEnd(int x, int y)
        {
            int endindex = RenderText.IsUnicode ? FontsLoader.Instance.CalculateCaretPosUnicode(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort) RenderText.FontStyle) : FontsLoader.Instance.CalculateCaretPosASCII(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort) RenderText.FontStyle);

            if (endindex == CaretIndex)
            {
                return;
            }

            CaretIndex = endindex;
            UpdateCaretPosition();
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