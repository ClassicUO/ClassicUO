using System.Collections.Generic;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    internal class ButtonTileArt : Button
    {
        private readonly ushort _hue;
        private readonly bool _isPartial;
        private readonly UOTexture _texture;
        private readonly int _tileX, _tileY;

        public ButtonTileArt(List<string> gparams) : base(gparams)
        {
            X = int.Parse(gparams[1]);
            Y = int.Parse(gparams[2]);
            ushort graphic = UInt16Converter.Parse(gparams[8]);
            _hue = UInt16Converter.Parse(gparams[9]);
            _tileX = int.Parse(gparams[10]);
            _tileY = int.Parse(gparams[11]);
            ContainsByBounds = true;
            IsFromServer = true;
            _texture = ArtLoader.Instance.GetTexture(graphic);

            if (_texture == null)
            {
                Dispose();

                return;
            }

            _isPartial = TileDataLoader.Instance.StaticData[graphic].IsPartialHue;
        }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

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

            ShaderHueTranslator.GetHueVector(ref HueVector, _hue, _isPartial, 0);

            return batcher.Draw2D(_texture, x + _tileX, y + _tileY, ref HueVector);
        }
    }
}