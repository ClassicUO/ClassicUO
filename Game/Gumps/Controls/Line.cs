using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class Line : GumpControl
    {
        private readonly SpriteTexture _texture;

        public Line(int x, int y, int w, int h, uint color)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _texture = new SpriteTexture(1, 1);

            _texture.SetData(new uint[1]
            {
                color
            });
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            _texture.Ticks = (long) totalMS;
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            return spriteBatch.Draw2D(_texture, new Rectangle((int) position.X, (int) position.Y, Width, Height), Vector3.Zero);
        }

        public override void Dispose()
        {
            _texture?.Dispose();
            base.Dispose();
        }
    }
}