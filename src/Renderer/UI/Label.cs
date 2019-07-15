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
    internal class Label : Control
    {
        private string _text;

        public Label(string text, int x, int y)
        {
            CanMove = true;
            AcceptMouseInput = false;


            X = x;
            Y = y;
            Text = text;
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;

                Vector2 size = Fonts.Regular.MeasureString(_text);
                Width = (int) size.X;
                Height = (int) size.Y;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            batcher.DrawString(Fonts.Regular, Text, x, y, ref _hueVector);

            return base.Draw(batcher, x, y);
        }
    }
}