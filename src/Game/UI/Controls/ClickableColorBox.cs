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

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    internal class ClickableColorBox : Control
    {
        private const int CELL = 12;

        private readonly ColorBox _colorBox;

        public ClickableColorBox(int x, int y, int w, int h, ushort hue, uint color)
        {
            X = x;
            Y = y;
            WantUpdateSize = false;

            GumpPic background = new GumpPic(0, 0, 0x00D4, 0);
            Add(background);
            _colorBox = new ColorBox(w, h, hue, color);
            _colorBox.X = 3;
            _colorBox.Y = 3;
            Add(_colorBox);

            Width = background.Width;
            Height = background.Height;
        }

        public ushort Hue => _colorBox.Hue;


        public void SetColor(ushort hue, uint pol)
        {
            _colorBox.SetColor(hue, pol);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                UIManager.GetGump<ColorPickerGump>()?.Dispose();

                ColorPickerGump pickerGump = new ColorPickerGump(0, 0, 100, 100, s => _colorBox.SetColor(s, HuesLoader.Instance.GetPolygoneColor(CELL, s)));
                UIManager.Add(pickerGump);
            }
        }
    }
}