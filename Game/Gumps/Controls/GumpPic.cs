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
    public abstract class GumpPicBase : GumpControl
    {
        private ushort _lastGump = 0xFFFF;

        protected GumpPicBase()
        {
            CanMove = true;
            AcceptMouseInput = true;
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

                if (Texture == null)
                {
                    Dispose();

                    return;
                }

                Width = Texture.Width;
                Height = Texture.Height;
            }

            base.Update(totalMS, frameMS);
        }

        protected override bool Contains(int x, int y)
        {
            return Texture.Contains(x, y);
            //return IO.Resources.Gumps.Contains(Graphic, x, y);
        }
    }

    public class GumpPic : GumpPicBase
    {
        public GumpPic(int x, int y, Graphic graphic, Hue hue)
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Hue = hue;

            Texture = IO.Resources.Gumps.GetGumpTexture(Graphic);
        }

        public bool IsPartialHue { get; set; }

        public GumpPic(string[] parts) : this(int.Parse(parts[1]), int.Parse(parts[2]), Graphic.Parse(parts[3]), parts.Length > 4 ? Hue.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1)) : (Hue) 0)
        {
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            spriteBatch.Draw2D(Texture, position, ShaderHuesTraslator.GetHueVector(Hue, IsPartialHue, 0, false));

            return base.Draw(spriteBatch, position, hue);
        }
    }
}