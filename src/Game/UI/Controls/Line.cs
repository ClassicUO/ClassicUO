#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System;

using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class Line : Control
    {
        private readonly Texture2D _texture;

        public Line(int x, int y, int w, int h, uint color)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _texture = Texture2DCache.GetTexture(new Color() { PackedValue = color });
        }

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
                g.Add(new Line(startx, starty, width, linewidth, linecolor), topage);

            g.Add(new Line(startx, starty, linewidth, height, linecolor), topage);
            g.Add(new Line(startx + width - 1, starty, linewidth, height, linecolor), topage);
            g.Add(new Line(startx, starty + height - 1, width, linewidth, linecolor), topage);

            return starty + height;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            ShaderHuesTraslator.GetHueVector(ref _hueVector, 0, false, Alpha);

            return batcher.Draw2D(_texture, x, y, Width, Height, ref _hueVector);
        }
    }
}