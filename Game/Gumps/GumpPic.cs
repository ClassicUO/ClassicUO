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
    public abstract class GumpPicBase : GumpControl
    {
        private Graphic _lastGump;

        public GumpPicBase() : base()
        {
            CanMove = true;
        }

        public Graphic Graphic { get; set; }
        public Hue Hue { get; set; }
        public bool IsPaperdoll { get; set; }


        public override void Update(double totalMS, double frameMS)
        {
            if (Texture == null || Texture.IsDisposed || Graphic != _lastGump)
            {
                _lastGump = Graphic;

                Texture = IO.Resources.Gumps.GetGumpTexture(Graphic);
                Width = Texture.Width;
                Height = Texture.Height;
            }

            Texture.Ticks = (long)totalMS;
            base.Update(totalMS, frameMS);
        }
    }

    public class GumpPic : GumpPicBase
    {

        public GumpPic(Graphic graphic) : base()
        {
            Graphic = graphic;
        }

        public GumpPic(string[] parts) : this(Graphic.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            if (parts.Length > 4)
                Hue = Hue.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1));
        }


        public override bool Draw(SpriteBatchUI spriteBatch,  Vector3 position)
        {
            spriteBatch.Draw2D(Texture, position, RenderExtentions.GetHueVector(Hue));
            return base.Draw(spriteBatch, position);
        }
    }
}
