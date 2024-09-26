#region license

// Copyright (c) 2024, andreakarasho
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
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    public class ResizePic : Control
    {
        private int _maxIndex;

        public ResizePic(ushort graphic)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            Graphic = graphic;

            for (_maxIndex = 0; _maxIndex < 9; ++_maxIndex)
            {
                if (Client.Game.UO.Gumps.GetGump((ushort)(Graphic + _maxIndex)).Texture == null)
                {
                    break;
                }
            }
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

            var texture0 = GetTexture(0, out var bounds0);
            var texture1 = GetTexture(1, out var bounds1);
            var texture2 = GetTexture(2, out var bounds2);
            var texture3 = GetTexture(3, out var bounds3);
            var texture4 = GetTexture(4, out var bounds4);
            var texture5 = GetTexture(5, out var bounds5);
            var texture6 = GetTexture(6, out var bounds6);
            var texture7 = GetTexture(7, out var bounds7);
            var texture8 = GetTexture(8, out var bounds8);

            int offsetTop = Math.Max(bounds0.Height, bounds2.Height) - bounds1.Height;
            int offsetBottom = Math.Max(bounds5.Height, bounds7.Height) - bounds6.Height;
            int offsetLeft = Math.Abs(Math.Max(bounds0.Width, bounds5.Width) - bounds2.Width);
            int offsetRight = Math.Max(bounds2.Width, bounds7.Width) - bounds4.Width;

            if (PixelsInXY(ref bounds0, Graphic, x, y))
            {
                return true;
            }

            int DW = Width - bounds0.Width - bounds2.Width;

            if (DW >= 1 && PixelsInXY(ref bounds1, (ushort)(Graphic + 1), x - bounds0.Width, y, DW))
            {
                return true;
            }

            if (
                PixelsInXY(
                    ref bounds2,
                    (ushort)(Graphic + 2),
                    x - (Width - bounds2.Width),
                    y - offsetTop
                )
            )
            {
                return true;
            }

            int DH = Height - bounds0.Height - bounds5.Height;

            if (
                DH >= 1
                && PixelsInXY(
                    ref bounds3,
                    (ushort)(Graphic + 3),
                    x /*- offsetLeft*/
                    ,
                    y - bounds0.Height,
                    0,
                    DH
                )
            )
            {
                return true;
            }

            DH = Height - bounds2.Height - bounds7.Height;

            if (
                DH >= 1
                && PixelsInXY(
                    ref bounds4,
                    (ushort)(Graphic + 5),
                    x
                        - (
                            Width - bounds4.Width /*- offsetRight*/
                        ),
                    y - bounds2.Height,
                    0,
                    DH
                )
            )
            {
                return true;
            }

            if (PixelsInXY(ref bounds5, (ushort)(Graphic + 6), x, y - (Height - bounds5.Height)))
            {
                return true;
            }

            DW = Width - bounds5.Width - bounds2.Width;

            if (
                DH >= 1
                && PixelsInXY(
                    ref bounds6,
                    (ushort)(Graphic + 7),
                    x - bounds5.Width,
                    y - (Height - bounds6.Height - offsetBottom),
                    DW
                )
            )
            {
                return true;
            }

            if (
                PixelsInXY(
                    ref bounds7,
                    (ushort)(Graphic + 8),
                    x - (Width - bounds7.Width),
                    y - (Height - bounds7.Height)
                )
            )
            {
                return true;
            }

            DW = Width - bounds0.Width - bounds2.Width;
            DW += offsetLeft + offsetRight;
            DH = Height - bounds2.Height - bounds7.Height;

            if (
                DW >= 1
                && DH >= 1
                && PixelsInXY(
                    ref bounds8,
                    (ushort)(Graphic + 4),
                    x - bounds0.Width,
                    y - bounds0.Height,
                    DW,
                    DH
                )
            )
            {
                return true;
            }

            return false;
        }

        private static bool PixelsInXY(
            ref Rectangle bounds,
            ushort graphic,
            int x,
            int y,
            int width = 0,
            int height = 0
        )
        {
            if (x < 0 || y < 0 || width > 0 && x >= width || height > 0 && y >= height)
            {
                return false;
            }

            if (bounds.Width == 0 || bounds.Height == 0)
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

            return Client.Game.UO.Gumps.PixelCheck(graphic, x, y);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (batcher.ClipBegin(x, y, Width, Height))
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha, true);

                DrawInternal(batcher, x, y, hueVector);
                base.Draw(batcher, x, y);

                batcher.ClipEnd();
            }

            return true;
        }

        private void DrawInternal(UltimaBatcher2D batcher, int x, int y, Vector3 color)
        {
            var texture0 = GetTexture(0, out var bounds0);
            var texture1 = GetTexture(1, out var bounds1);
            var texture2 = GetTexture(2, out var bounds2);
            var texture3 = GetTexture(3, out var bounds3);
            var texture4 = GetTexture(4, out var bounds4);
            var texture5 = GetTexture(5, out var bounds5);
            var texture6 = GetTexture(6, out var bounds6);
            var texture7 = GetTexture(7, out var bounds7);
            var texture8 = GetTexture(8, out var bounds8);

            int offsetTop = Math.Max(bounds0.Height, bounds2.Height) - bounds1.Height;
            int offsetBottom = Math.Max(bounds5.Height, bounds7.Height) - bounds6.Height;
            int offsetLeft = Math.Abs(Math.Max(bounds0.Width, bounds5.Width) - bounds2.Width);
            int offsetRight = Math.Max(bounds2.Width, bounds7.Width) - bounds4.Width;

            if (texture0 != null)
            {
                batcher.Draw(texture0, new Vector2(x, y), bounds0, color);
            }

            if (texture1 != null)
            {
                batcher.DrawTiled(
                    texture1,
                    new Rectangle(
                        x + bounds0.Width,
                        y,
                        Width - bounds0.Width - bounds2.Width,
                        bounds1.Height
                    ),
                    bounds1,
                    color
                );
            }

            if (texture2 != null)
            {
                batcher.Draw(
                    texture2,
                    new Vector2(x + (Width - bounds2.Width), y + offsetTop),
                    bounds2,
                    color
                );
            }

            if (texture3 != null)
            {
                batcher.DrawTiled(
                    texture3,
                    new Rectangle(
                        x,
                        y + bounds0.Height,
                        bounds3.Width,
                        Height - bounds0.Height - bounds5.Height
                    ),
                    bounds3,
                    color
                );
            }

            if (texture4 != null)
            {
                batcher.DrawTiled(
                    texture4,
                    new Rectangle(
                        x + (Width - bounds4.Width),
                        y + bounds2.Height,
                        bounds4.Width,
                        Height - bounds2.Height - bounds7.Height
                    ),
                    bounds4,
                    color
                );
            }

            if (texture5 != null)
            {
                batcher.Draw(
                    texture5,
                    new Vector2(x, y + (Height - bounds5.Height)),
                    bounds5,
                    color
                );
            }

            if (texture6 != null)
            {
                batcher.DrawTiled(
                    texture6,
                    new Rectangle(
                        x + bounds5.Width,
                        y + (Height - bounds6.Height - offsetBottom),
                        Width - bounds5.Width - bounds7.Width,
                        bounds6.Height
                    ),
                    bounds6,
                    color
                );
            }

            if (texture7 != null)
            {
                batcher.Draw(
                    texture7,
                    new Vector2(x + (Width - bounds7.Width), y + (Height - bounds7.Height)),
                    bounds7,
                    color
                );
            }

            if (texture8 != null)
            {
                batcher.DrawTiled(
                    texture8,
                    new Rectangle(
                        x + bounds0.Width,
                        y + bounds0.Height,
                        (Width - bounds0.Width - bounds2.Width) + (offsetLeft + offsetRight),
                        Height - bounds2.Height - bounds7.Height
                    ),
                    bounds8,
                    color
                );
            }
        }

        private Texture2D GetTexture(int index, out Rectangle bounds)
        {
            if (index >= 0 && index <= _maxIndex)
            {
                if (index >= 8)
                {
                    index = 4;
                }
                else if (index >= 4)
                {
                    ++index;
                }

                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(
                    (ushort)(Graphic + index)
                );

                bounds = gumpInfo.UV;
                return gumpInfo.Texture;
            }

            bounds = Rectangle.Empty;
            return null;
        }
    }
}
