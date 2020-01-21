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
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    internal class ClickableColorBox : ColorBox
    {
        private const int CELL = 12;

        private readonly UOTexture _background;

        public ClickableColorBox(int x, int y, int w, int h, ushort hue, uint color) : base(w, h, hue, color)
        {
            X = x + 3;
            Y = y + 3;
            WantUpdateSize = false;

            _background = GumpsLoader.Instance.GetTexture(0x00D4);
        }

        public override void Update(double totalMS, double frameMS)
        {
            _background.Ticks = (long) totalMS;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            batcher.Draw2D(_background, x - 3, y - 3, ref _hueVector);

            return base.Draw(batcher, x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                ColorPickerGump pickerGump = new ColorPickerGump(0, 0, 100, 100, s => SetColor(s, HuesLoader.Instance.GetPolygoneColor(CELL, s)));
                UIManager.Add(pickerGump);
            }
        }
    }
}
