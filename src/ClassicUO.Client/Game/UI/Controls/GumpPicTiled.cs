// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
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

        public GumpPicTiled(ushort graphic, GameContext context) : base(context)
        {
            CanMove = true;
            AcceptMouseInput = true;
            _graphic = graphic;

            InitializeSize();
        }

        public GumpPicTiled(int x, int y, int width, int heigth, ushort graphic, GameContext context) : this(graphic, context)
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

        public GumpPicTiled(List<string> parts, GameContext context) : this(UInt16Converter.Parse(parts[5]), context)
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
                    InitializeSize();
                }
            }
        }

        private void InitializeSize()
        {
            var uo = Context?.Game?.UO;
            if (uo == null)
                return;

            ref readonly var gumpInfo = ref uo.Gumps.GetGump(_graphic);

            if (gumpInfo.Texture == null)
            {
                Dispose();
                return;
            }

            if (Width == 0)
                Width = gumpInfo.UV.Width;
            if (Height == 0)
                Height = gumpInfo.UV.Height;
        }

        public ushort Hue { get; set; }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (IsDisposed || Context?.Game?.UO == null)
                return false;

            float layerDepth = layerDepthRef;
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

            ref readonly var gumpInfo = ref Context.Game.UO.Gumps.GetGump(Graphic);

            var texture = gumpInfo.Texture;
            if (texture != null)
            {
                var sourceRectangle = gumpInfo.UV;
                renderLists.AddGumpWithAtlas
                (
                    (batcher) =>
                    {
                        batcher.DrawTiled(
                            texture,
                            new Rectangle(x, y, Width, Height),
                            sourceRectangle,
                            hueVector,
                            layerDepth
                        );
                        return true;
                    }
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

            ref readonly var gumpInfo = ref Context.Game.UO.Gumps.GetGump(Graphic);

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

            return Context.Game.UO.Gumps.PixelCheck(Graphic, x, y);
        }
    }
}
