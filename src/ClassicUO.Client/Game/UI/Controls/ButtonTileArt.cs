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

            // The control is as big as the area it tiles, not as big as the tile.
            //
            // Button's constructor sizes itself from its gump art, which is right for a button
            // whose art *is* its face and wrong for this one: the whole point of buttontileart is
            // that a small piece of art covers a large region, and the last two arguments say how
            // large. Left at the art's size, ContainsByBounds tests a box the size of one tile,
            // so a row two hundred pixels wide made of an eight pixel strip is clickable for
            // eight pixels of it.
            //
            // The rest is not merely dead - it falls through to the gump behind, and a gump is
            // draggable, so pressing the middle of a list row or a slider picks the window up and
            // moves it. That is how this was found.
            if (_tileX > 0 && _tileY > 0)
            {
                Width = _tileX;
                Height = _tileY;
            }

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
