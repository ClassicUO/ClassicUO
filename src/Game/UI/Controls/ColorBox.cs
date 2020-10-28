using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    internal class ColorBox : Control
    {
        private Color _colorRGBA;
        private Texture2D _texture;

        public ColorBox(int width, int height, ushort hue, uint pol)
        {
            CanMove = false;

            SetColor(hue, pol);

            Width = width;
            Height = height;

            WantUpdateSize = false;
        }

        public ushort Hue { get; private set; }

        public void SetColor(ushort hue, uint pol)
        {
            Hue = hue;

            (byte b, byte g, byte r, byte a) = HuesHelper.GetBGRA(HuesHelper.RgbaToArgb(pol));

            _colorRGBA = new Color(a, b, g, r);

            if (_colorRGBA.A == 0)
            {
                _colorRGBA.A = 0xFF;
            }

            if (_texture == null || _texture.IsDisposed)
            {
                _texture = new UOTexture(1, 1);
            }

            _texture.SetData(new Color[1] { _colorRGBA });
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            return batcher.Draw2D(_texture, x, y, Width, Height, ref HueVector);
        }

        public override void Dispose()
        {
            _texture?.Dispose();
            base.Dispose();
        }
    }
}