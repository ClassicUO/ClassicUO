// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            var hueVector = ShaderHueTranslator.GetHueVector(_hue, _isPartial, 1f);

            ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);

            if (artInfo.Texture != null)
            {
                batcher.Draw(
                    artInfo.Texture,
                    new Vector2(x + _tileX, y + _tileY),
                    artInfo.UV,
                    hueVector
                );

                return true;
            }

            return false;
        }
    }
}
