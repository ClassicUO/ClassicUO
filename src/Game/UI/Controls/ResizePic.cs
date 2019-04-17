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

using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class ResizePic : Control
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

        public ResizePic(List<string> parts) : this(Graphic.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[4]);
            Height = int.Parse(parts[5]);
        }

        public bool OnlyCenterTransparent { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            foreach (SpriteTexture t in _gumpTexture)
                t.Ticks = (long) totalMS;

            base.Update(totalMS, frameMS);
        }

        protected override bool Contains(int x, int y)
        {
            for (int i = 0; i < 9; i++)
            {
                SpriteTexture t = _gumpTexture[i];
                int drawWidth = t.Width;
                int drawHeight = t.Height;
                int drawX = X;
                int drawY = Y;

                switch (i)
                {
                    case 0:
                        if (t.Contains(x, y))
                            return true;

                        break;
                    case 1:
                        drawX += _gumpTexture[0].Width;

                        int startX = drawX;
                        int maxW = drawX + (Width - _gumpTexture[0].Width - _gumpTexture[2].Width);
                        int maxH = drawY + drawHeight;


                        while (drawX < maxW)
                        {
                            if (t.Contains(Mouse.Position.X - drawX - ParentX, Mouse.Position.Y - drawY - ParentY))
                                return true;

                            drawX += drawWidth;

                            if (drawX > maxW)
                            {
                                drawX = startX;
                                drawY += drawHeight;

                                if (drawY > maxH)
                                    break;
                            }
                        }

                        break;
                    case 2:
                        drawX += Width - drawWidth;
                        if (t.Contains(Mouse.Position.X - drawX - ParentX, Mouse.Position.Y - drawY - ParentY))
                            return true;

                        break;
                    case 3:
                        drawY += _gumpTexture[0].Height;

                        startX = drawX;
                        maxW = drawX + drawWidth;
                        maxH = drawY + (Height - _gumpTexture[0].Height - _gumpTexture[5].Height);

                        while (drawX < maxW)
                        {
                            if (t.Contains(Mouse.Position.X - drawX - ParentX, Mouse.Position.Y - drawY - ParentY))
                                return true;

                            drawX += drawWidth;

                            if (drawX > maxW)
                            {
                                drawX = startX;
                                drawY += drawHeight;

                                if (drawY > maxH)
                                    break;
                            }
                        }
                        break;
                    case 4:
                        drawX += Width - drawWidth /*- offsetRight*/;
                        drawY += _gumpTexture[2].Height;

                        startX = drawX;
                        maxW = drawX + drawWidth;
                        maxH = drawY + (Height - _gumpTexture[2].Height - _gumpTexture[7].Height);


                        while (drawX < maxW)
                        {
                            if (t.Contains(Mouse.Position.X - drawX - ParentX, Mouse.Position.Y - drawY - ParentY))
                                return true;

                            drawX += drawWidth;

                            if (drawX > maxW)
                            {
                                drawX = startX;
                                drawY += drawHeight;

                                if (drawY > maxH)
                                    break;
                            }
                        }
                        break;
                    case 5:
                        drawY += Height - drawHeight;
                        if (t.Contains(Mouse.Position.X - drawX - ParentX, Mouse.Position.Y - drawY - ParentY))
                            return true;
                        break;
                    case 6:
                        drawX += _gumpTexture[5].Width;
                        drawY += Height - drawHeight /*- offsetBottom*/;

                        startX = drawX;
                        maxW = drawX + (Width - _gumpTexture[5].Width - _gumpTexture[7].Width);
                        maxH = drawY + drawHeight;


                        while (drawX < maxW)
                        {
                            if (t.Contains(Mouse.Position.X - drawX - ParentX, Mouse.Position.Y - drawY - ParentY))
                                return true;

                            drawX += drawWidth;

                            if (drawX > maxW)
                            {
                                drawX = startX;
                                drawY += drawHeight;

                                if (drawY > maxH)
                                    break;
                            }
                        }
                        break;
                    case 7:
                        drawX += Width - drawWidth;
                        drawY += Height - drawHeight;
                        if (t.Contains(Mouse.Position.X - drawX - ParentX, Mouse.Position.Y - drawY - ParentY))
                            return true;
                        break;
                    case 8:
                        drawX += _gumpTexture[0].Width;
                        drawY += _gumpTexture[0].Height;

                        startX = drawX;
                        maxW = drawX + (Width - _gumpTexture[0].Width - _gumpTexture[2].Width);
                        maxH = drawY + (Height - _gumpTexture[2].Height - _gumpTexture[7].Height);


                        while (drawX < maxW)
                        {                     
                            if (t.Contains(Mouse.Position.X - drawX - ParentX, Mouse.Position.Y - drawY - ParentY))
                                return true;

                            drawX += drawWidth;

                            if (drawX > maxW)
                            {
                                drawX = startX;
                                drawY += drawHeight;

                                if (drawY > maxH)
                                    break;
                            }


                        }

                        break;
                }
            }


            return false;
        }


        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            Vector3 hue = Vector3.Zero;
            if (IsTransparent)
                ShaderHuesTraslator.GetHueVector(ref hue, 0, false, IsTransparent ? Alpha : 0, false);
            DrawInternal(batcher, x, y, hue);
            return base.Draw(batcher, x, y);
        }

        private void DrawInternal(Batcher2D batcher, int x, int y, Vector3 color)
        {
            for (int i = 0; i < 9; i++)
            {
                SpriteTexture t = _gumpTexture[i];
                int drawWidth = t.Width;
                int drawHeight = t.Height;
                int drawX = x;
                int drawY = y;

                switch (i)
                {
                    case 0:
                        batcher.Draw2D(t, drawX, drawY, drawWidth, drawHeight, color);

                        break;
                    case 1:
                        drawX += _gumpTexture[0].Width;
                        drawWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, color);

                        break;
                    case 2:
                        drawX += Width - drawWidth;
                        batcher.Draw2D(t, drawX, drawY, drawWidth, drawHeight, color);

                        break;
                    case 3:
                        drawY += _gumpTexture[0].Height;
                        drawHeight = Height - _gumpTexture[0].Height - _gumpTexture[5].Height;
                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, color);

                        break;
                    case 4:
                        drawX += Width - drawWidth /*- offsetRight*/;
                        drawY += _gumpTexture[2].Height;
                        drawHeight = Height - _gumpTexture[2].Height - _gumpTexture[7].Height;
                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, color);

                        break;
                    case 5:
                        drawY += Height - drawHeight;
                        batcher.Draw2D(t, drawX, drawY, drawWidth, drawHeight, color);

                        break;
                    case 6:
                        drawX += _gumpTexture[5].Width;
                        drawY += Height - drawHeight /*- offsetBottom*/;
                        drawWidth = Width - _gumpTexture[5].Width - _gumpTexture[7].Width;
                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, color);

                        break;
                    case 7:
                        drawX += Width - drawWidth;
                        drawY += Height - drawHeight;
                        batcher.Draw2D(t, drawX, drawY, drawWidth, drawHeight, color);

                        break;
                    case 8:
                        drawX += _gumpTexture[0].Width;
                        drawY += _gumpTexture[0].Height;
                        drawWidth = Width - _gumpTexture[0].Width - _gumpTexture[2].Width;
                        drawHeight = Height - _gumpTexture[2].Height - _gumpTexture[7].Height;

                        //if (!OnlyCenterTransparent)
                        Vector3 c = color;

                        if (OnlyCenterTransparent)
                            c.Z = 1;

                        batcher.Draw2DTiled(t, drawX, drawY, drawWidth, drawHeight, c);

                        break;
                }
            }
        }
    }
}