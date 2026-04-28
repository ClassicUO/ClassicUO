// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal class GumpPicTiled : Control
    {
        private ushort _graphic;

        public GumpPicTiled(ushort graphic)
        {
            CanMove = true;
            AcceptMouseInput = true;
            Graphic = graphic;
        }

        public GumpPicTiled(int x, int y, int width, int heigth, ushort graphic) : this(graphic)
        {
            X = x;
            Y = y;

            if (width > 0)
            {
                Width = width;
            }

            if (heigth > 0)
            {
                Height = heigth;
            }
        }

        public GumpPicTiled(List<string> parts) : this(UInt16Converter.Parse(parts[5]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
            IsFromServer = true;
        }

        public ushort Graphic
        {
            get => _graphic;
            set
            {
                if (_graphic != value && value != 0xFFFF)
                {
                    _graphic = value;

                    ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(_graphic);

                    if (gumpInfo.Texture == null)
                    {
                        Dispose();

                        return;
                    }

                    Width = gumpInfo.UV.Width;
                    Height = gumpInfo.UV.Height;
                }
            }
        }

        public ushort Hue { get; set; }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);

            if (gumpInfo.Texture != null)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

                renderLists.AddGumpSpriteTiled(
                    gumpInfo.Texture,
                    gumpInfo.UV,
                    new Rectangle(x, y, Width, Height),
                    hueVector,
                    layerDepthRef
                );
            }

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }

        public override bool Contains(int x, int y)
        {
            int width = Width;
            int height = Height;

            x -= Offset.X;
            y -= Offset.Y;

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);

            if (gumpInfo.Texture == null)
            {
                return false;
            }

            if (width == 0)
            {
                width = gumpInfo.UV.Width;
            }

            if (height == 0)
            {
                height = gumpInfo.UV.Height;
            }

            while (x > gumpInfo.UV.Width && width > gumpInfo.UV.Width)
            {
                x -= gumpInfo.UV.Width;
                width -= gumpInfo.UV.Width;
            }

            while (y > gumpInfo.UV.Height && height > gumpInfo.UV.Height)
            {
                y -= gumpInfo.UV.Height;
                height -= gumpInfo.UV.Height;
            }

            if (x > width || y > height)
            {
                return false;
            }

            return Client.Game.UO.Gumps.PixelCheck(Graphic, x, y);
        }
    }
}
