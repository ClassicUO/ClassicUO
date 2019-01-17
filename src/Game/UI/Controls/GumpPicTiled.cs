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

        public GumpPicTiled(string[] parts) : this(Graphic.Parse(parts[5]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
        }

        public Graphic Graphic { get; set; }

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

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            batcher.Draw2DTiled(Texture, new Rectangle(position.X, position.Y, Width, Height), ShaderHuesTraslator.GetHueVector(0, false, IsTransparent ? Alpha : 0, false));

            return base.Draw(batcher, position, hue);
        }
    }
}