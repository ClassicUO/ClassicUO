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

using System;
using System.Collections.Generic;

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class ResizePic : Control
    {
        private readonly UOTexture[] _gumpTexture = new UOTexture[9];

        public ResizePic(Graphic graphic)
        {
            CanMove = true;
            CanCloseWithRightClick = true;

            for (int i = 0; i < _gumpTexture.Length; i++)
            {
                UOTexture t = FileManager.Gumps.GetTexture((Graphic) (graphic + i));

                if (t == null)
                {
                    Dispose();

                    return;
                }

                if (i == 4)
                    _gumpTexture[8] = t;
                else if (i > 4)
                    _gumpTexture[i - 1] = t;
                else
                    _gumpTexture[i] = t;
            }

            Graphic = graphic;
        }

        public ResizePic(List<string> parts) : this(Graphic.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[4]);
            Height = int.Parse(parts[5]);
        }

        public Graphic Graphic { get; }

        public bool OnlyCenterTransparent { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            foreach (UOTexture t in _gumpTexture)
                t.Ticks = (long) totalMS;

            base.Update(totalMS, frameMS);
        }

        public override bool Contains(int x, int y)
        {
            UOTexture[] th = _gumpTexture;

            int offsetTop = Math.Max(th[0].Height, th[2].Height) - th[1].Height;
            int offsetBottom = Math.Max(th[5].Height, th[7].Height) - th[6].Height;
            int offsetLeft = Math.Max(th[0].Width, th[5].Width) - th[3].Width;
            int offsetRight = Math.Max(th[2].Width, th[7].Width) - th[4].Width;


            for (int i = 0; i < 9; i++)
            {
                switch (i)
                {
                    case 0:
                        if (PixelsInXY(FileManager.Gumps.GetTexture(Graphic), x, y))
                            return true;
                        break;
                    case 1:
                        int DW = Width - th[0].Width - th[2].Width;
                        if (DW < 1)
                            break;

                        if (PixelsInXY(FileManager.Gumps.GetTexture((ushort) (Graphic + 1)), x - th[0].Width, y, DW, 0))
                            return true;

                        break;
                    case 2:

                        if (PixelsInXY(FileManager.Gumps.GetTexture((ushort)(Graphic + 2)), x - (Width - th[i].Width), y - offsetTop))
                            return true;
                    
                        break;
                    case 3:

                        int DH = Height - th[0].Height - th[5].Height;
                        if (DH < 1)
                            break;

                        if (PixelsInXY(FileManager.Gumps.GetTexture((ushort)(Graphic + 3)), x - offsetLeft, y - th[0].Height, 0, DH))
                            return true;


                        break;
                    case 4:

                        DH = Height - th[2].Height - th[7].Height;
                        if (DH < 1)
                            break;

                        if (PixelsInXY(FileManager.Gumps.GetTexture((ushort)(Graphic + 5)), x - (Width - th[i].Width - offsetRight), y - th[2].Height, 0, DH))
                            return true;

                        break;
                    case 5:

                        if (PixelsInXY(FileManager.Gumps.GetTexture((ushort)(Graphic + 6)), x, y - (Height - th[i].Height)))
                            return true;

                        break;
                    case 6:

                        DW = Width - th[5].Width - th[2].Width;
                        if (DW < 1)
                            break;

                        if (PixelsInXY(FileManager.Gumps.GetTexture((ushort)(Graphic + 7)), x - th[5].Width, y - (Height - th[i].Height - offsetBottom), DW, 0))
                            return true;


                        break;
                    case 7:

                        if (PixelsInXY(FileManager.Gumps.GetTexture((ushort)(Graphic + 8)), x - (Width - th[i].Width), y - (Height - th[i].Height)))
                            return true;

                        break;
                    case 8:

                        DW = Width - th[0].Width - th[2].Width;
                        if (DW < 1)
                            break;

                        DH = Height - th[2].Height - th[7].Height;
                        if (DH < 1)
                            break;

                        if (PixelsInXY(FileManager.Gumps.GetTexture((ushort)(Graphic + 4)), x - th[0].Width, y - th[0].Height, DW, DH))
                            return true;


                        break;
                }
            }

            return false;
        }


        private static bool PixelsInXY(UOTexture texture, int x, int y, int width = 0, int height = 0)
        {
            if (x < 0 || y < 0 || (width > 0 && x >= width) || (height > 0 && y >= height))
                return false;

            int textureWidth = texture.Width;
            int textureHeight = texture.Height;

            if (width == 0)
                width = textureWidth;

            if (height == 0)
                height = textureHeight;


            while (x > textureWidth && width > textureWidth)
            {
                x -= textureWidth;
                width -= textureWidth;
            }

            if (x < 0 || x > width)
                return false;

            while (y > textureHeight && height > textureHeight)
            {
                y -= textureHeight;
                height -= textureHeight;
            }

            if (y < 0 || y > height)
                return false;
            
            return texture.Contains(x, y);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            var rect = ScissorStack.CalculateScissors(Matrix.Identity, x, y, Width, Height);

            if (ScissorStack.PushScissors(rect))
            {
                ShaderHuesTraslator.GetHueVector(ref _hueVector, 0, false, Alpha, true);

                batcher.EnableScissorTest(true);

                DrawInternal(batcher, x, y, ref _hueVector);
                base.Draw(batcher, x, y);

                batcher.EnableScissorTest(false);
                ScissorStack.PopScissors();

                return true;
            }

            return false;
        }

        private void DrawInternal(UltimaBatcher2D batcher, int x, int y, ref Vector3 color)
        {
            int offsetTop = Math.Max(_gumpTexture[0].Height, _gumpTexture[2].Height) - _gumpTexture[1].Height;
            int offsetBottom = Math.Max(_gumpTexture[5].Height, _gumpTexture[7].Height) - _gumpTexture[6].Height;
            int offsetLeft = Math.Max(_gumpTexture[0].Width, _gumpTexture[5].Width) - _gumpTexture[3].Width;
            int offsetRight = Math.Max(_gumpTexture[2].Width, _gumpTexture[7].Width) - _gumpTexture[4].Width;

            for (int i = 0; i < 9; i++)
            {
                UOTexture t = _gumpTexture[i];
                int drawWidth = t.Width;
                int drawHeight = t.Height;
                int drawX = x;
                int drawY = y;

                switch (i)
                {
                    case 0:

                        batcher.Draw2D(t, drawX, drawY, drawWidth, drawHeight, ref color);
                        break;

                    case 1:
                        drawX += _gumpTexture[0].Width;
                        drawWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, ref color);

                        break;

                    case 2:
                        drawX += Width - drawWidth;
                        drawY += offsetTop;
                        batcher.Draw2D(t, drawX, drawY, drawWidth, drawHeight, ref color);

                        break;

                    case 3:
                        drawX += offsetLeft;
                        drawY += _gumpTexture[0].Height;
                        drawHeight = Height - _gumpTexture[0].Height - _gumpTexture[5].Height;
                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, ref color);

                        break;

                    case 4:
                        drawX += Width - drawWidth - offsetRight;
                        drawY += _gumpTexture[2].Height;
                        drawHeight = Height - _gumpTexture[2].Height - _gumpTexture[7].Height;
                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, ref color);

                        break;

                    case 5:
                        drawY += Height - drawHeight;
                        batcher.Draw2D(t, drawX, drawY, drawWidth, drawHeight, ref color);

                        break;

                    case 6:
                        drawX += _gumpTexture[5].Width;
                        drawY += Height - drawHeight - offsetBottom;
                        drawWidth = Width - _gumpTexture[5].Width - _gumpTexture[7].Width;
                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, ref color);

                        break;

                    case 7:
                        drawX += Width - drawWidth;
                        drawY += Height - drawHeight;
                        batcher.Draw2D(t, drawX, drawY, drawWidth, drawHeight, ref color);

                        break;

                    case 8:
                        drawX += _gumpTexture[0].Width;
                        drawY += _gumpTexture[0].Height;
                        drawWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
                        drawHeight = Height - _gumpTexture[2].Height - _gumpTexture[7].Height;

                        if (OnlyCenterTransparent)
                            color.Z = 1;

                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, ref color);

                        break;
                }
            }
        }
    }
}