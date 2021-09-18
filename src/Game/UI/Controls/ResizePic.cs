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
        public ResizePic(ushort graphic)
        {
            CanMove = true;
            CanCloseWithRightClick = true;

            for (int i = 0; i < 9; i++)
            {
                if (GumpsLoader.Instance.GetGumpTexture((ushort)(graphic + i), out _) == null)
                {
                    Dispose();
                    return;
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


        public override bool Contains(int x, int y)
        {
            x -= Offset.X;
            y -= Offset.Y;


            _ = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 0), out var bounds0);
            _ = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 1), out var bounds1);
            _ = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 2), out var bounds2);
            _ = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 3), out var bounds3);
            _ = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 5), out var bounds4);
            _ = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 6), out var bounds5);
            _ = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 7), out var bounds6);
            _ = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 8), out var bounds7);
            _ = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 4), out var bounds8);

            int offsetTop = Math.Max(bounds0.Height, bounds2.Height) - bounds1.Height;
            int offsetBottom = Math.Max(bounds5.Height, bounds7.Height) - bounds6.Height;
            int offsetLeft = Math.Abs(Math.Max(bounds0.Width, bounds5.Width) - bounds2.Width);
            int offsetRight = Math.Max(bounds2.Width, bounds7.Width) - bounds4.Width;


            for (int i = 0; i < 9; i++)
            {
                switch (i)
                {
                    case 0:
                        if (PixelsInXY(ref bounds0, Graphic, x, y))
                        {
                            return true;
                        }

                        break;

                    case 1:
                        int DW = Width - bounds0.Width - bounds2.Width;

                        if (DW < 1)
                        {
                            break;
                        }

                        if (PixelsInXY(ref bounds1, (ushort)(Graphic + 1), x - bounds0.Width, y, DW))
                        {
                            return true;
                        }

                        break;

                    case 2:

                        if (PixelsInXY(ref bounds2, (ushort)(Graphic + 2), x - (Width - bounds2.Width), y - offsetTop))
                        {
                            return true;
                        }

                        break;

                    case 3:

                        int DH = Height - bounds0.Height - bounds5.Height;

                        if (DH < 1)
                        {
                            break;
                        }

                        if (PixelsInXY
                        (
                            ref bounds3, (ushort)(Graphic + 3),
                            x /*- offsetLeft*/,
                            y - bounds0.Height,
                            0,
                            DH
                        ))
                        {
                            return true;
                        }


                        break;

                    case 4:

                        DH = Height - bounds2.Height - bounds7.Height;

                        if (DH < 1)
                        {
                            break;
                        }

                        if (PixelsInXY
                        (
                            ref bounds5, (ushort)(Graphic + 5),
                            x - (Width - bounds4.Width /*- offsetRight*/),
                            y - bounds2.Height,
                            0,
                            DH
                        ))
                        {
                            return true;
                        }

                        break;

                    case 5:

                        if (PixelsInXY(ref bounds6, (ushort)(Graphic + 6), x, y - (Height - bounds5.Height)))
                        {
                            return true;
                        }

                        break;

                    case 6:

                        DW = Width - bounds5.Width - bounds2.Width;

                        if (DW < 1)
                        {
                            break;
                        }

                        if (PixelsInXY(ref bounds7, (ushort)(Graphic + 7), x - bounds5.Width, y - (Height - bounds6.Height - offsetBottom), DW))
                        {
                            return true;
                        }


                        break;

                    case 7:

                        if (PixelsInXY(ref bounds8, (ushort)(Graphic + 8), x - (Width - bounds7.Width), y - (Height - bounds7.Height)))
                        {
                            return true;
                        }

                        break;

                    case 8:

                        DW = Width - bounds0.Width - bounds2.Width;

                        DW += offsetLeft + offsetRight;

                        if (DW < 1)
                        {
                            break;
                        }

                        DH = Height - bounds2.Height - bounds7.Height;

                        if (DH < 1)
                        {
                            break;
                        }

                        if (PixelsInXY
                        (
                            ref bounds4, 
                            (ushort)(Graphic + 4),
                            x - bounds0.Width,
                            y - bounds0.Height,
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


        private static bool PixelsInXY(ref Rectangle bounds, ushort graphic, int x, int y, int width = 0, int height = 0)
        {
            if (x < 0 || y < 0 || width > 0 && x >= width || height > 0 && y >= height)
            {
                return false;
            }

            int textureWidth = bounds.Width;
            int textureHeight = bounds.Height;

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
            var texture0 = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 0), out var bounds0);
            var texture1 = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 1), out var bounds1);
            var texture2 = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 2), out var bounds2);
            var texture3 = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 3), out var bounds3);
            var texture4 = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 5), out var bounds4);
            var texture5 = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 6), out var bounds5);
            var texture6 = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 7), out var bounds6);
            var texture7 = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 8), out var bounds7);
            var texture8 = GumpsLoader.Instance.GetGumpTexture((ushort)(Graphic + 4), out var bounds8);

            int offsetTop = Math.Max(bounds0.Height, bounds2.Height) - bounds1.Height;
            int offsetBottom = Math.Max(bounds5.Height, bounds7.Height) - bounds6.Height;
            int offsetLeft = Math.Abs(Math.Max(bounds0.Width, bounds5.Width) - bounds2.Width);
            int offsetRight = Math.Max(bounds2.Width, bounds7.Width) - bounds4.Width;




            batcher.Draw2D
            (
                texture0,
                x,
                y,
                bounds0.X,
                bounds0.Y,
                bounds0.Width,
                bounds0.Height,
                ref color
            );


            batcher.Draw2DTiled
            (
                texture1,
                x + bounds0.Width,
                y,
                Width - bounds0.Width - bounds2.Width,
                bounds1.Height,
                bounds1.X,
                bounds1.Y,
                bounds1.Width,
                bounds1.Height,
                ref color
            );


            batcher.Draw2D
            (
                texture2,
                x + (Width - bounds2.Width),
                y + offsetTop,
                bounds2.X,
                bounds2.Y,
                bounds2.Width,
                bounds2.Height,
                ref color
            );


            batcher.Draw2DTiled
            (
                texture3,
                x,
                y + bounds0.Height,
                bounds3.Width,
                Height - bounds0.Height - bounds5.Height,
                bounds3.X,
                bounds3.Y,
                bounds3.Width,
                bounds3.Height,
                ref color
            );


            batcher.Draw2DTiled
            (
                texture4,
                x + (Width - bounds4.Width),
                y + bounds2.Height,
                bounds4.Width,
                Height - bounds2.Height - bounds7.Height,
                bounds4.X,
                bounds4.Y,
                bounds4.Width,
                bounds4.Height,
                ref color
            );


            batcher.Draw2D
            (
                texture5,
                x,
                y + (Height - bounds5.Height),
                bounds5.X,
                bounds5.Y,
                bounds5.Width,
                bounds5.Height,
                ref color
            );


            batcher.Draw2DTiled
            (
                texture6,
                x + bounds5.Width,
                y + (Height - bounds6.Height - offsetBottom),
                Width - bounds5.Width - bounds7.Width,
                bounds6.Height,
                bounds6.X,
                bounds6.Y,
                bounds6.Width,
                bounds6.Height,
                ref color
            );


            batcher.Draw2D
            (
                texture7,
                x + (Width - bounds7.Width),
                y + (Height - bounds7.Height),
                bounds7.X,
                bounds7.Y,
                bounds7.Width,
                bounds7.Height,
                ref color
            );


            batcher.Draw2DTiled
            (
                texture8,
                x + bounds0.Width,
                y + bounds0.Height,
                (Width - bounds0.Width - bounds2.Width) + (offsetLeft + offsetRight),
                Height - bounds2.Height - bounds7.Height,
                bounds8.X,
                bounds8.Y,
                bounds8.Width,
                bounds8.Height,
                ref color
            );
        }
    }
}