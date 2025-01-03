// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, IsPartialHue, 1);

            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(Graphic);

            if (artInfo.Texture != null)
            {
                batcher.Draw(
                    artInfo.Texture,
                    new Rectangle(x, y, Width, Height),
                    artInfo.UV,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            return Client.Game.UO.Arts.PixelCheck(Graphic, x - Offset.X, y - Offset.Y);
        }
    }
}
