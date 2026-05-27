// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal class StaticPic : Control
    {
        private ushort _graphic;

        public StaticPic(ushort graphic, ushort hue)
        {
            Hue = hue;
            Graphic = graphic;
            CanMove = true;
            WantUpdateSize = false;
        }

        public StaticPic(List<string> parts)
            : this(
                UInt16Converter.Parse(parts[3]),
                parts.Count > 4 ? UInt16Converter.Parse(parts[4]) : (ushort)0
            )
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            IsFromServer = true;
        }

        public ushort Hue { get; set; }
        public bool IsPartialHue { get; set; }

        public ushort Graphic
        {
            get => _graphic;
            set
            {
                _graphic = value;

                ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(value);

                if (artInfo.Texture == null)
                {
                    Dispose();

                    return;
                }

                Width = artInfo.UV.Width;
                Height = artInfo.UV.Height;

                IsPartialHue = Client.Game.UO.FileManager.TileData.StaticData[value].IsPartialHue;
            }
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            float layerDepth = layerDepthRef;
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, IsPartialHue, 1);

            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(Graphic);

            var texture = artInfo.Texture;
            if (texture != null)
            {
                var sourceRectangle = artInfo.UV;
                renderLists.AddGumpWithAtlas
                (
                    (batcher) =>
                    {
                        batcher.Draw(
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
            return Client.Game.UO.Arts.PixelCheck(Graphic, x - Offset.X, y - Offset.Y);
        }
    }
}
