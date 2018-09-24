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
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class GumpPicTiled : GumpControl
    {
        private readonly Graphic _graphic;


        public GumpPicTiled(Graphic graphic)
        {
            _graphic = graphic;
            CanMove = true;
            Texture = IO.Resources.Gumps.GetGumpTexture(graphic);
        }

        public GumpPicTiled(int x, int y, int width, int heigth, Graphic graphic)
        {
            X = x;
            Y = y;
            Width = width;
            Height = heigth;
            _graphic = graphic;
            CanMove = true;
            Texture = IO.Resources.Gumps.GetGumpTexture(graphic);
        }

        public GumpPicTiled(string[] parts) : this(Graphic.Parse(parts[5]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
        }


        public override void Update(double totalMS, double frameMS)
        {
            Texture.Ticks = (long) totalMS;
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            spriteBatch.Draw2DTiled(Texture, new Rectangle((int) position.X, (int) position.Y, Width, Height),
                RenderExtentions.GetHueVector(0, false, IsTransparent, false));
            return base.Draw(spriteBatch, position, hue);
        }
    }
}