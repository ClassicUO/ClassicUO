using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    class ButtonTileArt : Button
    {
        private readonly int _tileX, _tileY;
        private readonly ushort _hue;
        private readonly UOTexture16 _texture;
        private readonly bool _isPartial;

        public ButtonTileArt(List<string> gparams) : 
            base(gparams)
        {
            X = int.Parse(gparams[1]);
            Y = int.Parse(gparams[2]);
            ushort graphic = UInt16Converter.Parse(gparams[8]);
            _hue = UInt16Converter.Parse(gparams[9]);
            _tileX = int.Parse(gparams[10]);
            _tileY = int.Parse(gparams[11]);
            ContainsByBounds = true;

            _texture = ArtLoader.Instance.GetTexture(graphic);

            if (_texture == null)
            {
                Dispose();
                return;
            }

            _isPartial = TileDataLoader.Instance.StaticData[graphic].IsPartialHue;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (_texture != null)
            {
                _texture.Ticks = Time.Ticks;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            base.Draw(batcher, x, y);

            ResetHueVector();

            ShaderHuesTraslator.GetHueVector(ref _hueVector, _hue, _isPartial, 0);

            return batcher.Draw2D(_texture, x + _tileX, y + _tileY, ref _hueVector);
        }
    }
}
