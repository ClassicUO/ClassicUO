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

using ClassicUO.Game.UI.Controls;

using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer.UI
{
    internal class Panel : Control
    {
        private readonly Color _color;

        public Panel(int x, int y, int w, int h, Color color)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;
            _color = color;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 zero = Vector3.Zero;
            batcher.Draw2D(Textures.GetTexture(_color), x, y, Width, Height, ref zero);

            return base.Draw(batcher, x, y);
        }
    }
}