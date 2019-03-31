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

using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.UI.Controls
{
    internal class Line : Control
    {
        internal static int CreateRectangleArea(Gump g, int startx, int starty, int width, int height, int topage = 0, uint linecolor = 0xAFAFAF, int linewidth = 1, string toplabel = null, ushort textcolor = 999, byte textfont = 0xFF)
        {
            if (!string.IsNullOrEmpty(toplabel))
            {
                Label l = new Label(toplabel, true, textcolor, font: textfont);
                int rwidth = (width - l.Width) >> 1;
                l.X = startx + rwidth + 2;
                l.Y = Math.Max(0, starty - ((l.Height + 1) >> 1));
                g.Add(l, topage);
                if (rwidth > 0)
                {
                    g.Add(new Line(startx, starty, rwidth, linewidth, linecolor), topage);
                    g.Add(new Line(startx + width - rwidth, starty, rwidth, linewidth, linecolor), topage);
                }
            }
            else
            {
                g.Add(new Line(startx, starty, width, linewidth, linecolor), topage);
            }
            g.Add(new Line(startx, starty, linewidth, height, linecolor), topage);
            g.Add(new Line(startx + width - 1, starty, linewidth, height, linecolor), topage);
            g.Add(new Line(startx, starty + height - 1, width, linewidth, linecolor), topage);
            return starty + height;
        }

        private readonly SpriteTexture _texture;

        public Line(int x, int y, int w, int h, uint color)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _texture = new SpriteTexture(1, 1);

            _texture.SetData(new uint[1]
            {
                color
            });
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            _texture.Ticks = (long)totalMS;
        }

        public override bool Draw(Batcher2D batcher, Point position)
        {
            return batcher.Draw2D(_texture, new Rectangle(position.X, position.Y, Width, Height), ShaderHuesTraslator.GetHueVector(0, false, IsTransparent ? Alpha : 0, false));
        }

        public override void Dispose()
        {
            _texture?.Dispose();
            base.Dispose();
        }
    }
}