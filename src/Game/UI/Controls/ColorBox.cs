using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ColorBox : Control
    {
        private Color _colorRGBA;

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
                _colorRGBA.A = 0xFF;

            if (Texture == null || Texture.IsDisposed)
                Texture = new SpriteTexture(1, 1);
            Texture.SetData(new Color[1] {_colorRGBA});
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Texture != null && !Texture.IsDisposed)
                Texture.Ticks = (long) totalMS;
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            return batcher.Draw2D(Texture, x, y, Width, Height, Vector3.Zero);
        }

        public override void Dispose()
        {
            Texture?.Dispose();
            base.Dispose();
        }
    }
}