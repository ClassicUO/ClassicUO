// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    internal class ButtonTileArt : Button
    {
        private readonly ushort _hue;
        private readonly bool _isPartial;
        private readonly int _tileX,
            _tileY;
        private ushort _graphic;

        public ButtonTileArt(List<string> gparams) : base(gparams)
        {
            X = int.Parse(gparams[1]);
            Y = int.Parse(gparams[2]);
            _graphic = UInt16Converter.Parse(gparams[8]);
            _hue = UInt16Converter.Parse(gparams[9]);
            _tileX = int.Parse(gparams[10]);
            _tileY = int.Parse(gparams[11]);
            ContainsByBounds = true;
            IsFromServer = true;

            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);

            if (artInfo.Texture == null)
            {
                Dispose();

                return;
            }

            _isPartial = Client.Game.UO.FileManager.TileData.StaticData[_graphic].IsPartialHue;
        }

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            float layerDepth = layerDepthRef;

            var hueVector = ShaderHueTranslator.GetHueVector(_hue, _isPartial, 1f);

            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);

            if (artInfo.Texture != null)
            {
                var texture = artInfo.Texture;
                var sourceRectangle = artInfo.UV;
                renderLists.AddGumpWithAtlas
                (
                    (batcher) =>
                    {
                        batcher.Draw(
                            texture,
                            new Vector2(x + _tileX, y + _tileY),
                            sourceRectangle,
                            hueVector,
                            layerDepth
                        );
                        return true;
                    }
                );

                return true;
            }

            return false;
        }
    }
}
