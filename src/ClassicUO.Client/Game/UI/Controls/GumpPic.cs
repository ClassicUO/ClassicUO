// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal abstract class GumpPicBase : Control
    {
        private ushort _graphic;

        protected GumpPicBase(GameContext context) : base(context)
        {
            CanMove = true;
            AcceptMouseInput = true;
        }

        public ushort Graphic
        {
            get => _graphic;
            set
            {
                _graphic = value;

                var uo = Context?.Game?.UO;
                if (uo == null)
                    return;

                ref readonly var gumpInfo = ref uo.Gumps.GetGump(_graphic);

                if (gumpInfo.Texture == null)
                {
                    Dispose();
                    return;
                }

                Width = gumpInfo.UV.Width;
                Height = gumpInfo.UV.Height;
            }
        }

        protected bool EnsureSize()
        {
            // Now that we have eager init via Graphic setter, just check validity
            if (Context?.Game?.UO == null)
                return false;

            ref readonly var gumpInfo = ref Context.Game.UO.Gumps.GetGump(_graphic);
            if (gumpInfo.Texture == null)
                return false;

            return !IsDisposed;
        }

        public ushort Hue { get; set; }
        public bool IsPartialHue { get; set; }


        public override bool Contains(int x, int y)
        {
            var uo = Context?.Game?.UO;
            if (uo == null)
                return false;

            ref readonly var gumpInfo = ref uo.Gumps.GetGump(_graphic);

            if (gumpInfo.Texture == null)
            {
                return false;
            }

            if (uo.Gumps.PixelCheck(Graphic, x - Offset.X, y - Offset.Y))
            {
                return true;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                Control c = Children[i];

                // might be wrong x, y. They should be calculated by position
                if (c.Contains(x, y))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal class GumpPic : GumpPicBase
    {
        public GumpPic(int x, int y, ushort graphic, ushort hue, GameContext context) : base(context)
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Hue = hue;
            IsFromServer = true;
        }

        public GumpPic(List<string> parts, GameContext context)
            : this(
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                UInt16Converter.Parse(parts[3]),
                (ushort)(
                    parts.Count > 4
                        ? TransformHue(
                            (ushort)(
                                UInt16Converter.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1))
                                + 1
                            )
                        )
                        : 0
                ),
                context
            )
        { }

        public bool ContainsByBounds { get; set; }

        public override bool Contains(int x, int y)
        {
            return ContainsByBounds || base.Contains(x, y);
        }

        private static ushort TransformHue(ushort hue)
        {
            if (hue <= 2)
            {
                hue = 0;
            }

            return hue;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (!EnsureSize())
                return false;

            float layerDepth = layerDepthRef;

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, IsPartialHue, Alpha, true);

            ref readonly var gumpInfo = ref Context.Game.UO.Gumps.GetGump(Graphic);

            if (gumpInfo.Texture != null)
            {
                var texture = gumpInfo.Texture;
                var sourceRectangle = gumpInfo.UV;
                renderLists.AddGumpWithAtlas(
                    batcher =>
                    {
                        batcher.Draw(
                            texture,
                            new Rectangle(x, y, Width, Height),
                            sourceRectangle,
                            hueVector,
                            layerDepth
                        );
                        return true;
                    });
            }

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }
    }

    internal class VirtueGumpPic : GumpPic
    {
        private readonly World _world;

        public VirtueGumpPic(World world, List<string> parts) : base(parts, world?.Context)
        {
            _world = world;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _world.Network.Send_VirtueGumpResponse(_world.Player, Graphic);

                return true;
            }

            return base.OnMouseDoubleClick(x, y, button);
        }
    }

    internal class GumpPicInPic : GumpPicBase
    {
        private readonly Rectangle _picInPicBounds;

        public GumpPicInPic(
            int x,
            int y,
            ushort graphic,
            ushort sx,
            ushort sy,
            ushort width,
            ushort height,
            GameContext context
        ) : base(context)
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Width = width;
            Height = height;
            _picInPicBounds = new Rectangle(sx, sy, Width, Height);
            IsFromServer = true;
        }

        public GumpPicInPic(List<string> parts, GameContext context)
            : this(
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                UInt16Converter.Parse(parts[3]),
                UInt16Converter.Parse(parts[4]),
                UInt16Converter.Parse(parts[5]),
                UInt16Converter.Parse(parts[6]),
                UInt16Converter.Parse(parts[7]),
                context
            )
        { }

        public override bool Contains(int x, int y)
        {
            return true;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            if (!EnsureSize())
                return false;

            float layerDepth = layerDepthRef;

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, IsPartialHue, Alpha, true);

            ref readonly var gumpInfo = ref Context.Game.UO.Gumps.GetGump(Graphic);

            var sourceBounds = new Rectangle(gumpInfo.UV.X + _picInPicBounds.X, gumpInfo.UV.Y + _picInPicBounds.Y, _picInPicBounds.Width, _picInPicBounds.Height);

            var texture = gumpInfo.Texture;
            if (texture != null)
            {
                renderLists.AddGumpWithAtlas(
                    batcher =>
                    {
                        batcher.Draw(
                            texture,
                            new Rectangle(x, y, Width, Height),
                            sourceBounds,
                            hueVector,
                            layerDepth
                        );
                        return true;
                    }
                );
            }

            return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
        }
    }
}
