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

namespace ClassicUO.Game.Gumps.Controls
{
    public class ResizePic : GumpControl
    {
        private readonly Graphic _graphic;
        private readonly SpriteTexture[] _gumpTexture = new SpriteTexture[9];

        public ResizePic(Graphic graphic)
        {
            _graphic = graphic;
            CanMove = true;
            CanCloseWithRightClick = true;

            for (int i = 0; i < _gumpTexture.Length; i++)
            {
                if (_gumpTexture[i] == null)
                    _gumpTexture[i] = IO.Resources.Gumps.GetGumpTexture((Graphic) (_graphic + i));
            }
        }

        public ResizePic(string[] parts) : this(Graphic.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[4]);
            Height = int.Parse(parts[5]);
        }

        public override void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _gumpTexture.Length; i++)
                _gumpTexture[i].Ticks = (long) totalMS;
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            int centerWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
            int centerHeight = Height - _gumpTexture[0].Height - _gumpTexture[6].Height;
            int line2Y = (int) position.Y + _gumpTexture[0].Height;
            int line3Y = (int) position.Y + Height - _gumpTexture[6].Height;
            Vector3 color = IsTransparent ? RenderExtentions.GetHueVector(0, false, .5f, true) : Vector3.Zero;

            // top row
            spriteBatch.Draw2D(_gumpTexture[0], new Vector3(position.X, position.Y, 0), color);
            spriteBatch.Draw2DTiled(_gumpTexture[1], new Rectangle((int) position.X + _gumpTexture[0].Width, (int) position.Y, centerWidth, _gumpTexture[0].Height), color);
            spriteBatch.Draw2D(_gumpTexture[2], new Vector3(position.X + Width - _gumpTexture[2].Width, position.Y, 0), color);

            // middle
            spriteBatch.Draw2DTiled(_gumpTexture[3], new Rectangle((int) position.X, line2Y, _gumpTexture[3].Width, centerHeight), color);
            spriteBatch.Draw2DTiled(_gumpTexture[4], new Rectangle((int) position.X + _gumpTexture[3].Width, line2Y, centerWidth, centerHeight), color);
            spriteBatch.Draw2DTiled(_gumpTexture[5], new Rectangle((int) position.X + Width - _gumpTexture[5].Width, line2Y, _gumpTexture[5].Width, centerHeight), color);

            // bottom
            spriteBatch.Draw2D(_gumpTexture[6], new Vector3(position.X, line3Y, 0), color);
            spriteBatch.Draw2DTiled(_gumpTexture[7], new Rectangle((int) position.X + _gumpTexture[6].Width, line3Y, centerWidth, _gumpTexture[6].Height), color);
            spriteBatch.Draw2D(_gumpTexture[8], new Vector3(position.X + Width - _gumpTexture[8].Width, line3Y, 0), color);

            return base.Draw(spriteBatch, position, hue);
        }
    }
}