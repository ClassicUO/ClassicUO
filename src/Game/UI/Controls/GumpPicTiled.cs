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

using System.Collections.Generic;

using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class GumpPicTiled : Control
    {
        private Graphic _lastGraphic;

        public GumpPicTiled(Graphic graphic)
        {
            CanMove = true;
            AcceptMouseInput = true;
            Texture = FileManager.Gumps.GetTexture(graphic);
            Graphic = _lastGraphic = graphic;
        }

        public GumpPicTiled(int x, int y, int width, int heigth, Graphic graphic) : this(graphic)
        {
            X = x;
            Y = y;
            Width = width;
            Height = heigth;
        }

        public GumpPicTiled(List<string> parts) : this(Graphic.Parse(parts[5]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
        }

        internal GumpPicTiled(int x, int y, int width, int heigth, UOTexture texture)
        {
            CanMove = true;
            AcceptMouseInput = true;
            X = x;
            Y = y;
            Width = width;
            Height = heigth;
            Graphic = _lastGraphic = Graphic.INVALID;
            Texture = texture;
        }

        public Graphic Graphic { get; set; }

        public Hue Hue { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            if (_lastGraphic != Graphic)
            {
                Texture = FileManager.Gumps.GetTexture(Graphic);
                _lastGraphic = Graphic;
            }

            Texture.Ticks = (long) totalMS;
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, Hue, false, Alpha, true);

            batcher.Draw2DTiled(Texture, x, y, Width, Height, ref _hueVector);

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            int width = Width;
            int height = Height;

            if (width == 0)
                width = Texture.Width;

            if (height == 0)
                height = Texture.Height;

            while (x > Texture.Width && width > Texture.Width)
            {
                x -= Texture.Width;
                width -= Texture.Width;
            }

            while (y > Texture.Height && height > Texture.Height)
            {
                y -= Texture.Height;
                height -= Texture.Height;
            }


            if (x > width || y > height)
                return false;


            return Texture.Contains(x, y);
        }
    }
}