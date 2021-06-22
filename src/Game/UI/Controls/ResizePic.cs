#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ResizePic : Control
    {
        private readonly UOTexture[] _gumpTexture = new UOTexture[9];

        public ResizePic(ushort graphic)
        {
            CanMove = true;
            CanCloseWithRightClick = true;

            for (int i = 0; i < _gumpTexture.Length; i++)
            {
                UOTexture t = GumpsLoader.Instance.GetTexture((ushort) (graphic + i));

                if (t == null)
                {
                    return;
                }

                if (i == 4)
                {
                    _gumpTexture[8] = t;
                }
                else if (i > 4)
                {
                    _gumpTexture[i - 1] = t;
                }
                else
                {
                    _gumpTexture[i] = t;
                }
            }

            Graphic = graphic;
        }

        public ResizePic(List<string> parts) : this(UInt16Converter.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[4]);
            Height = int.Parse(parts[5]);
            IsFromServer = true;
        }

        public ushort Graphic { get; }

        public override void Update(double totalTime, double frameTime)
        {
            for (int i = 0; i < _gumpTexture.Length; i++)
            {
                if (_gumpTexture[i] != null)
                {
                    _gumpTexture[i].Ticks = (long) totalTime;
                }
            }

            base.Update(totalTime, frameTime);
        }

        public override bool Contains(int x, int y)
        {
            x -= Offset.X;
            y -= Offset.Y;

            int th_0_width = _gumpTexture[0]?.Width ?? 0;

            int th_0_height = _gumpTexture[0]?.Height ?? 0;

            int th_1_width = _gumpTexture[1]?.Width ?? 0;

            int th_1_height = _gumpTexture[1]?.Height ?? 0;

            int th_2_width = _gumpTexture[2]?.Width ?? 0;

            int th_2_height = _gumpTexture[2]?.Height ?? 0;

            int th_3_width = _gumpTexture[3]?.Width ?? 0;

            int th_3_height = _gumpTexture[3]?.Height ?? 0;

            int th_4_width = _gumpTexture[4]?.Width ?? 0;

            int th_4_height = _gumpTexture[4]?.Height ?? 0;

            int th_5_width = _gumpTexture[5]?.Width ?? 0;

            int th_5_height = _gumpTexture[5]?.Height ?? 0;

            int th_6_width = _gumpTexture[6]?.Width ?? 0;

            int th_6_height = _gumpTexture[6]?.Height ?? 0;

            int th_7_width = _gumpTexture[7]?.Width ?? 0;

            int th_7_height = _gumpTexture[7]?.Height ?? 0;

            int th_8_width = _gumpTexture[8]?.Width ?? 0;

            int th_8_height = _gumpTexture[8]?.Height ?? 0;


            int offsetTop = Math.Max(th_0_height, th_2_height) - th_1_height;
            int offsetBottom = Math.Max(th_5_height, th_7_height) - th_6_height;
            int offsetLeft = Math.Abs(Math.Max(th_0_width, th_5_width) - th_2_width);
            int offsetRight = Math.Max(th_2_width, th_7_width) - th_4_width;


            for (int i = 0; i < 9; i++)
            {
                if (_gumpTexture[i] == null)
                {
                    continue;
                }

                switch (i)
                {
                    case 0:
                        if (PixelsInXY(GumpsLoader.Instance.GetTexture(Graphic), Graphic, x, y))
                        {
                            return true;
                        }

                        break;

                    case 1:
                        int DW = Width - th_0_width - th_2_width;

                        if (DW < 1)
                        {
                            break;
                        }

                        if (PixelsInXY(GumpsLoader.Instance.GetTexture((ushort) (Graphic + 1)), (ushort)(Graphic + 1), x - th_0_width, y, DW))
                        {
                            return true;
                        }

                        break;

                    case 2:

                        if (PixelsInXY(GumpsLoader.Instance.GetTexture((ushort) (Graphic + 2)), (ushort)(Graphic + 2), x - (Width - th_2_width), y - offsetTop))
                        {
                            return true;
                        }

                        break;

                    case 3:

                        int DH = Height - th_0_height - th_5_height;

                        if (DH < 1)
                        {
                            break;
                        }

                        if (PixelsInXY
                        (
                            GumpsLoader.Instance.GetTexture((ushort) (Graphic + 3)), (ushort)(Graphic + 3),
                            x /*- offsetLeft*/,
                            y - th_0_height,
                            0,
                            DH
                        ))
                        {
                            return true;
                        }


                        break;

                    case 4:

                        DH = Height - th_2_height - th_7_height;

                        if (DH < 1)
                        {
                            break;
                        }

                        if (PixelsInXY
                        (
                            GumpsLoader.Instance.GetTexture((ushort) (Graphic + 5)), (ushort)(Graphic + 5),
                            x - (Width - th_4_width /*- offsetRight*/),
                            y - th_2_height,
                            0,
                            DH
                        ))
                        {
                            return true;
                        }

                        break;

                    case 5:

                        if (PixelsInXY(GumpsLoader.Instance.GetTexture((ushort) (Graphic + 6)), (ushort)(Graphic + 6), x, y - (Height - th_5_height)))
                        {
                            return true;
                        }

                        break;

                    case 6:

                        DW = Width - th_5_width - th_2_width;

                        if (DW < 1)
                        {
                            break;
                        }

                        if (PixelsInXY(GumpsLoader.Instance.GetTexture((ushort) (Graphic + 7)), (ushort)(Graphic + 7), x - th_5_width, y - (Height - th_6_height - offsetBottom), DW))
                        {
                            return true;
                        }


                        break;

                    case 7:

                        if (PixelsInXY(GumpsLoader.Instance.GetTexture((ushort) (Graphic + 8)), (ushort)(Graphic + 8), x - (Width - th_7_width), y - (Height - th_7_height)))
                        {
                            return true;
                        }

                        break;

                    case 8:

                        DW = Width - th_0_width - th_2_width;

                        DW += offsetLeft + offsetRight;

                        if (DW < 1)
                        {
                            break;
                        }

                        DH = Height - th_2_height - th_7_height;

                        if (DH < 1)
                        {
                            break;
                        }

                        if (PixelsInXY
                        (
                            GumpsLoader.Instance.GetTexture((ushort) (Graphic + 4)), 
                            (ushort)(Graphic + 4),
                            x - th_0_width,
                            y - th_0_height,
                            DW,
                            DH
                        ))
                        {
                            return true;
                        }


                        break;
                }
            }

            return false;
        }


        private static bool PixelsInXY(UOTexture texture, ushort graphic, int x, int y, int width = 0, int height = 0)
        {
            if (x < 0 || y < 0 || width > 0 && x >= width || height > 0 && y >= height)
            {
                return false;
            }

            int textureWidth = texture.Width;
            int textureHeight = texture.Height;

            if (width == 0)
            {
                width = textureWidth;
            }

            if (height == 0)
            {
                height = textureHeight;
            }


            while (x >= textureWidth && width >= textureWidth)
            {
                x -= textureWidth;
                width -= textureWidth;
            }

            if (x < 0 || x > width)
            {
                return false;
            }

            while (y >= textureHeight && height >= textureHeight)
            {
                y -= textureHeight;
                height -= textureHeight;
            }

            if (y < 0 || y > height)
            {
                return false;
            }

            return GumpsLoader.Instance.PixelCheck(graphic, x, y);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            if (batcher.ClipBegin(x, y, Width, Height))
            {
                ShaderHueTranslator.GetHueVector
                (
                    ref HueVector,
                    0,
                    false,
                    Alpha,
                    true
                );

                DrawInternal(batcher, x, y, ref HueVector);
                base.Draw(batcher, x, y);

                batcher.ClipEnd();
            }
            
            return true;
        }

        private void DrawInternal(UltimaBatcher2D batcher, int x, int y, ref Vector3 color)
        {
            int th_0_width = _gumpTexture[0]?.Width ?? 0;

            int th_0_height = _gumpTexture[0]?.Height ?? 0;

            int th_1_width = _gumpTexture[1]?.Width ?? 0;

            int th_1_height = _gumpTexture[1]?.Height ?? 0;

            int th_2_width = _gumpTexture[2]?.Width ?? 0;

            int th_2_height = _gumpTexture[2]?.Height ?? 0;

            int th_3_width = _gumpTexture[3]?.Width ?? 0;

            int th_3_height = _gumpTexture[3]?.Height ?? 0;

            int th_4_width = _gumpTexture[4]?.Width ?? 0;

            int th_4_height = _gumpTexture[4]?.Height ?? 0;

            int th_5_width = _gumpTexture[5]?.Width ?? 0;

            int th_5_height = _gumpTexture[5]?.Height ?? 0;

            int th_6_width = _gumpTexture[6]?.Width ?? 0;

            int th_6_height = _gumpTexture[6]?.Height ?? 0;

            int th_7_width = _gumpTexture[7]?.Width ?? 0;

            int th_7_height = _gumpTexture[7]?.Height ?? 0;

            int th_8_width = _gumpTexture[8]?.Width ?? 0;

            int th_8_height = _gumpTexture[8]?.Height ?? 0;


            int offsetTop = Math.Max(th_0_height, th_2_height) - th_1_height;
            int offsetBottom = Math.Max(th_5_height, th_7_height) - th_6_height;
            int offsetLeft = Math.Abs(Math.Max(th_0_width, th_5_width) - th_2_width);
            int offsetRight = Math.Max(th_2_width, th_7_width) - th_4_width;


            for (int i = 0; i < 9; i++)
            {
                UOTexture t = _gumpTexture[i];

                if (t == null)
                {
                    continue;
                }

                int drawWidth = t.Width;
                int drawHeight = t.Height;
                int drawX = x;
                int drawY = y;

                switch (i)
                {
                    case 0:

                        batcher.Draw2D
                        (
                            t,
                            drawX,
                            drawY,
                            drawWidth,
                            drawHeight,
                            ref color
                        );

                        break;

                    case 1:
                        drawX += th_0_width;
                        drawWidth = Width - th_0_width - th_2_width;

                        batcher.Draw2DTiled
                        (
                            t,
                            drawX,
                            drawY,
                            drawWidth,
                            drawHeight,
                            ref color
                        );

                        break;

                    case 2:
                        drawX += Width - drawWidth;
                        drawY += offsetTop;

                        batcher.Draw2D
                        (
                            t,
                            drawX,
                            drawY,
                            drawWidth,
                            drawHeight,
                            ref color
                        );

                        break;

                    case 3:
                        //drawX += offsetLeft;
                        drawY += th_0_height;
                        drawHeight = Height - th_0_height - th_5_height;

                        batcher.Draw2DTiled
                        (
                            t,
                            drawX,
                            drawY,
                            drawWidth,
                            drawHeight,
                            ref color
                        );

                        break;

                    case 4:
                        drawX += Width - drawWidth /*- offsetRight*/;
                        drawY += th_2_height;
                        drawHeight = Height - th_2_height - th_7_height;

                        batcher.Draw2DTiled
                        (
                            t,
                            drawX,
                            drawY,
                            drawWidth,
                            drawHeight,
                            ref color
                        );

                        break;

                    case 5:
                        drawY += Height - drawHeight;

                        batcher.Draw2D
                        (
                            t,
                            drawX,
                            drawY,
                            drawWidth,
                            drawHeight,
                            ref color
                        );

                        break;

                    case 6:
                        drawX += th_5_width;
                        drawY += Height - drawHeight - offsetBottom;
                        drawWidth = Width - th_5_width - th_7_width;

                        batcher.Draw2DTiled
                        (
                            t,
                            drawX,
                            drawY,
                            drawWidth,
                            drawHeight,
                            ref color
                        );

                        break;

                    case 7:
                        drawX += Width - drawWidth;
                        drawY += Height - drawHeight;

                        batcher.Draw2D
                        (
                            t,
                            drawX,
                            drawY,
                            drawWidth,
                            drawHeight,
                            ref color
                        );

                        break;

                    case 8:
                        drawX += th_0_width;
                        drawY += th_0_height;
                        drawWidth = Width - th_0_width - th_2_width;
                        drawHeight = Height - th_2_height - th_7_height;

                        drawWidth += offsetLeft + offsetRight;

                        batcher.Draw2DTiled
                        (
                            t,
                            drawX,
                            drawY,
                            drawWidth,
                            drawHeight,
                            ref color
                        );

                        break;
                }
            }
        }
    }
}