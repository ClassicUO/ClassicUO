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

using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class StaticPic : GumpControl
    {
        private readonly Graphic _graphic;

        private bool _isPartial;

        public StaticPic(Graphic graphic, Hue hue)
        {
            _graphic = graphic;
            Hue = hue;

            _isPartial = IO.Resources.TileData.IsPartialHue((long) IO.Resources.TileData.StaticData[_graphic].Flags);

            CanMove = true;
        }

        public StaticPic(string[] parts) : this(Graphic.Parse(parts[3]),
            parts.Length > 4 ? Hue.Parse(parts[4]) : (Hue) 0)
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
        }

        public Hue Hue { get; set; }


        public override void Update(double totalMS, double frameMS)
        {
            if (Texture == null || Texture.IsDisposed)
            {
                Texture = Art.GetStaticTexture(_graphic);
                Width = Texture.Width;
                Height = Texture.Height;
            }

            Texture.Ticks = (long) totalMS;
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            spriteBatch.Draw2D(Texture, position, RenderExtentions.GetHueVector(Hue, _isPartial, false, true));
            return base.Draw(spriteBatch, position, hue);
        }
    }
}