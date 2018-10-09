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

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class CheckerTrans : GumpControl
    {
        private static SpriteTexture _transparentTexture;

        public CheckerTrans() => AcceptMouseInput = false;

        public CheckerTrans(string[] parts) : this()
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
        }

        public static SpriteTexture TransparentTexture
        {
            get
            {
                if (_transparentTexture == null)
                {
                    _transparentTexture = new SpriteTexture(1, 1);
                    _transparentTexture.SetData(new Color[1] {Color.Black});
                }

                _transparentTexture.Ticks = CoreGame.Ticks;
                return _transparentTexture;
            }
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null) =>
            spriteBatch.Draw2D(TransparentTexture, new Rectangle((int) position.X, (int) position.Y, Width, Height),
                RenderExtentions.GetHueVector(0, false, .5f, true));
    }
}