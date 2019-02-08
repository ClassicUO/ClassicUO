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
    abstract class AbstractEntry : IDisposable
    {
        protected AbstractEntry(int maxcharlength, int width, int maxWidth)
        {
            MaxCharCount =/* maxcharlength <= 0 ? 200 :*/ maxcharlength;
            Width = width;
            MaxWidth = maxWidth;
        }

        public void Dispose()
        {
            RenderText?.Dispose();
            RenderText = null;
            RenderCaret?.Dispose();
            RenderCaret = null;
        }

        public RenderedText RenderText { get; protected set; }

        public RenderedText RenderCaret { get; protected set; }

        public Point CaretPosition { get; set; }
        public int CaretIndex { get; protected set; }

        public abstract string Text { get; set; }

        public bool IsChanged { get; protected set; }

        public int MaxCharCount { get; }

        public int Width { get; }
        public int Height => RenderText.Height < 25 ? 25 : RenderText.Height;

        public int MaxWidth { get; }

        public int Offset { get; set; }

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
                (x, y) = FileManager.Fonts.GetCaretPosUnicode(RenderText.Font, RenderText.Text, CaretIndex, Width, RenderText.Align, (ushort)RenderText.FontStyle);
            else
                (x, y) = FileManager.Fonts.GetCaretPosASCII(RenderText.Font, RenderText.Text, CaretIndex, Width, RenderText.Align, (ushort)RenderText.FontStyle);
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
                CaretIndex = FileManager.Fonts.CalculateCaretPosUnicode(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort)RenderText.FontStyle);
            else
                CaretIndex = FileManager.Fonts.CalculateCaretPosASCII(RenderText.Font, RenderText.Text, x, y, Width, RenderText.Align, (ushort)RenderText.FontStyle);

            if (oldPos != CaretIndex)
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
