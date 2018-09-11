#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Views
{
    public class TextOverheadView : View
    {
        private RenderedText _text;

        public TextOverheadView(TextOverhead parent, int maxwidth = 0, ushort hue = 0xFFFF, byte font = 0, bool isunicode = false, FontStyle style = FontStyle.None) : base(parent)
        {
            _text = new RenderedText(parent.Text)
            {
                MaxWidth = maxwidth, Hue = hue, Font = font, IsUnicode = isunicode, FontStyle = style
            };

            Texture = _text.Texture;
        }

        public override bool Draw(SpriteBatch3D spriteBatch, Vector3 position)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
                return false;

            Texture.Ticks = World.Ticks;
   
            return base.Draw(spriteBatch, position);
        }

    }
}
