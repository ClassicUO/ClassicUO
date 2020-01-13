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

using Microsoft.Xna.Framework.Input;

using Mouse = ClassicUO.Input.Mouse;

namespace ClassicUO.Game.UI.Controls
{
    internal class DataBox : Control
    {
        public DataBox(int x, int y, int w, int h)
        {
            CanMove = false;
            AcceptMouseInput = true;
            X = x;
            Y = y;
            Width = w;
            Height = h;
            WantUpdateSize = false;
        }

        public bool ContainsByBounds;


        public override bool Contains(int x, int y)
        {
            if (ContainsByBounds)
                return true;

            Control t = null;
            x += ScreenCoordinateX;
            y += ScreenCoordinateY;

            foreach (Control child in Children)
            {
                child.HitTest(x, y, ref t);

                if (t != null)
                    return true;
            }

            return false;
        }
    }
}