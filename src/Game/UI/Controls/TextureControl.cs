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

using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class TextureControl : Control
    {
        public TextureControl()
        {
            CanMove = true;
            AcceptMouseInput = true;
            ScaleTexture = true;
        }

        public bool ScaleTexture { get; set; }

        public Hue Hue { get; set; }
        public bool IsPartial { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Texture != null)
                Texture.Ticks = Engine.Ticks;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Texture == null)
                return false;

            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, Hue, IsPartial, Alpha);

            if (ScaleTexture)
            {
                if (Texture is ArtTexture artTexture)
                {
                    int w = Width;
                    int h = Height;
                    var r = artTexture.ImageRectangle;

                    if (r.Width < Width)
                    {
                        w = r.Width;
                        x += (Width >> 1) - (w >> 1);
                    }

                    if (r.Height < Height)
                    {
                        h = r.Height;
                        y += (Height >> 1) - (h >> 1);
                    }

                    return batcher.Draw2D(Texture, x, y, w, h, r.X, r.Y, r.Width, r.Height, ref _hueVector);
                }

                return batcher.Draw2D(Texture, x, y, Width, Height, 0, 0, Texture.Width, Texture.Height, ref _hueVector);
            }

            return batcher.Draw2D(Texture, x, y, ref _hueVector);
        }
    }
}