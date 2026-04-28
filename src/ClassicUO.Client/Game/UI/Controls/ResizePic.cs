// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal class ResizePic : Control
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

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            // Clip everything drawn inside this 9-slice to the control bounds. Children
            // emit directly into the parent stream so the owning gump's retained cache
            // can observe them (previously this was a closure spinning up a throwaway
            // RenderLists, which defeated caching).
            renderLists.PushClip(new Rectangle(x, y, Width, Height));

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha, true);
            EmitNineSlice(renderLists, x, y, hueVector, layerDepthRef);

            base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);

            renderLists.PopClip();

            return true;
        }

        // 9-slice layout:
        //    0  1  2       corners  = simple Sprite draw
        //    3  8  4       middle   = SpriteTiled (3,4,1,6) and the centre (8)
        //    5  6  7
        private void EmitNineSlice(RenderLists renderLists, int x, int y, Vector3 color, float layerDepth)
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

            // Top-left corner
            if (texture0 != null)
            {
                renderLists.AddGumpSprite(
                    texture0, bounds0,
                    new Rectangle(x, y, bounds0.Width, bounds0.Height),
                    color, layerDepth);
            }

            // Top edge (tiled)
            if (texture1 != null)
            {
                renderLists.AddGumpSpriteTiled(
                    texture1, bounds1,
                    new Rectangle(
                        x + bounds0.Width,
                        y,
                        Width - bounds0.Width - bounds2.Width,
                        bounds1.Height),
                    color, layerDepth);
            }

            // Top-right corner
            if (texture2 != null)
            {
                renderLists.AddGumpSprite(
                    texture2, bounds2,
                    new Rectangle(
                        x + (Width - bounds2.Width),
                        y + offsetTop,
                        bounds2.Width,
                        bounds2.Height),
                    color, layerDepth);
            }

            // Left edge (tiled)
            if (texture3 != null)
            {
                renderLists.AddGumpSpriteTiled(
                    texture3, bounds3,
                    new Rectangle(
                        x,
                        y + bounds0.Height,
                        bounds3.Width,
                        Height - bounds0.Height - bounds5.Height),
                    color, layerDepth);
            }

            // Right edge (tiled)
            if (texture4 != null)
            {
                renderLists.AddGumpSpriteTiled(
                    texture4, bounds4,
                    new Rectangle(
                        x + (Width - bounds4.Width),
                        y + bounds2.Height,
                        bounds4.Width,
                        Height - bounds2.Height - bounds7.Height),
                    color, layerDepth);
            }

            // Bottom-left corner
            if (texture5 != null)
            {
                renderLists.AddGumpSprite(
                    texture5, bounds5,
                    new Rectangle(
                        x,
                        y + (Height - bounds5.Height),
                        bounds5.Width,
                        bounds5.Height),
                    color, layerDepth);
            }

            // Bottom edge (tiled)
            if (texture6 != null)
            {
                renderLists.AddGumpSpriteTiled(
                    texture6, bounds6,
                    new Rectangle(
                        x + bounds5.Width,
                        y + (Height - bounds6.Height - offsetBottom),
                        Width - bounds5.Width - bounds7.Width,
                        bounds6.Height),
                    color, layerDepth);
            }

            // Bottom-right corner
            if (texture7 != null)
            {
                renderLists.AddGumpSprite(
                    texture7, bounds7,
                    new Rectangle(
                        x + (Width - bounds7.Width),
                        y + (Height - bounds7.Height),
                        bounds7.Width,
                        bounds7.Height),
                    color, layerDepth);
            }

            // Centre (tiled)
            if (texture8 != null)
            {
                renderLists.AddGumpSpriteTiled(
                    texture8, bounds8,
                    new Rectangle(
                        x + bounds0.Width,
                        y + bounds0.Height,
                        (Width - bounds0.Width - bounds2.Width) + (offsetLeft + offsetRight),
                        Height - bounds2.Height - bounds7.Height),
                    color, layerDepth);
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
