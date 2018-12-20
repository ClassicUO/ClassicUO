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

namespace ClassicUO.Game.Gumps.Controls
{
    public class ResizePic : Control
    {
        private readonly SpriteTexture[] _gumpTexture = new SpriteTexture[9];

        public ResizePic(Graphic graphic)
        {
            CanMove = true;
            CanCloseWithRightClick = true;

            for (int i = 0; i < _gumpTexture.Length; i++)
            {
                SpriteTexture t = FileManager.Gumps.GetTexture((Graphic) (graphic + i));

                if (t == null)
                {
                    Dispose();

                    return;
                }

                if (i == 4)
                    _gumpTexture[8] = t;
                else if (i > 4)
                    _gumpTexture[i - 1] = t;
                else _gumpTexture[i] = t;
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

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            //int offsetTop = Math.Max(_gumpTexture[0].Height, _gumpTexture[2].Height) - _gumpTexture[1].Height;
            //int offsetBottom = Math.Max(_gumpTexture[5].Height, _gumpTexture[7].Height) - _gumpTexture[6].Height;
            //int offsetLeft = Math.Max(_gumpTexture[0].Width, _gumpTexture[5].Width) - _gumpTexture[3].Width;
            //int offsetRight = Math.Max(_gumpTexture[2].Width, _gumpTexture[7].Width) - _gumpTexture[4].Width;
            Vector3 color = IsTransparent ? ShaderHuesTraslator.GetHueVector(0, false, Alpha, true) : Vector3.Zero;

            for (int i = 0; i < 9; i++)
            {
                SpriteTexture t = _gumpTexture[i];
                int drawWidth = t.Width;
                int drawHeight = t.Height;
                int drawX = position.X;
                int drawY = position.Y;

                switch (i)
                {
                    case 0:
                        batcher.Draw2D(t, new Rectangle(drawX, drawY, drawWidth, drawHeight), color);

                        break;
                    case 1:
                        drawX += _gumpTexture[0].Width;
                        drawWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
                        batcher.Draw2DTiled(t, new Rectangle(drawX, drawY, drawWidth, drawHeight), color);

                        break;
                    case 2:
                        drawX += Width - drawWidth;
                        batcher.Draw2D(t, new Rectangle(drawX, drawY, drawWidth, drawHeight), color);

                        break;
                    case 3:
                        drawY += _gumpTexture[0].Height;
                        drawHeight = Height - _gumpTexture[0].Height - _gumpTexture[5].Height;
                        batcher.Draw2DTiled(t, new Rectangle(drawX, drawY, drawWidth, drawHeight), color);

                        break;
                    case 4:
                        drawX += Width - drawWidth /*- offsetRight*/;
                        drawY += _gumpTexture[2].Height;
                        drawHeight = Height - _gumpTexture[2].Height - _gumpTexture[7].Height;
                        batcher.Draw2DTiled(t, new Rectangle(drawX, drawY, drawWidth, drawHeight), color);

                        break;
                    case 5:
                        drawY += Height - drawHeight;
                        batcher.Draw2D(t, new Rectangle(drawX, drawY, drawWidth, drawHeight), color);

                        break;
                    case 6:
                        drawX += _gumpTexture[5].Width;
                        drawY += Height - drawHeight /*- offsetBottom*/;
                        drawWidth = Width - _gumpTexture[5].Width - _gumpTexture[7].Width;
                        batcher.Draw2DTiled(t, new Rectangle(drawX, drawY, drawWidth, drawHeight), color);

                        break;
                    case 7:
                        drawX += Width - drawWidth;
                        drawY += Height - drawHeight;
                        batcher.Draw2D(t, new Rectangle(drawX, drawY, drawWidth, drawHeight), color);

                        break;
                    case 8:
                        drawX += _gumpTexture[0].Width;
                        drawY += _gumpTexture[0].Height;
                        drawWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
                        drawHeight = Height - _gumpTexture[2].Height - _gumpTexture[7].Height;
                        batcher.Draw2DTiled(t, new Rectangle(drawX, drawY, drawWidth, drawHeight), color);

                        break;
                }
            }

            //int centerWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
            //int centerHeight = Height - _gumpTexture[0].Height - _gumpTexture[6].Height;
            //int line2Y = position.Y + _gumpTexture[0].Height;
            //int line3Y = position.Y + Height - _gumpTexture[6].Height;
            //Vector3 color = IsTransparent ? ShaderHuesTraslator.GetHueVector(0, false, Alpha, true) : Vector3.Zero;

            //// top row
            //batcher.Draw2D(_gumpTexture[0], position, color);
            //batcher.Draw2DTiled(_gumpTexture[1], new Rectangle(position.X + _gumpTexture[0].Width, position.Y, centerWidth, _gumpTexture[0].Height), color);
            //batcher.Draw2D(_gumpTexture[2], new Point(position.X + Width - _gumpTexture[2].Width, position.Y), color);

            //// middle
            //batcher.Draw2DTiled(_gumpTexture[3], new Rectangle(position.X, line2Y, _gumpTexture[3].Width, centerHeight), color);
            //batcher.Draw2DTiled(_gumpTexture[4], new Rectangle(position.X + _gumpTexture[3].Width, line2Y, centerWidth, centerHeight), color);
            //batcher.Draw2DTiled(_gumpTexture[5], new Rectangle(position.X + Width - _gumpTexture[5].Width, line2Y, _gumpTexture[5].Width, centerHeight), color);

            //// bottom
            //batcher.Draw2D(_gumpTexture[6], new Point(position.X, line3Y), color);
            //batcher.Draw2DTiled(_gumpTexture[7], new Rectangle(position.X + _gumpTexture[6].Width, line3Y, centerWidth, _gumpTexture[6].Height), color);
            //batcher.Draw2D(_gumpTexture[8], new Point(position.X + Width - _gumpTexture[8].Width, line3Y), color);

            return base.Draw(batcher, position, hue);
        }
    }
}