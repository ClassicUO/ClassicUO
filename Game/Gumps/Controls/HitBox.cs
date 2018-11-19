using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    class HitBox : GumpControl
    {
        private readonly SpriteTexture _texture;

        public HitBox(int x, int y, int w, int h) : base()
        {
            CanMove = false;
            AcceptMouseInput = true;
            Alpha = 0.75f;
            IsTransparent = true;
            _texture = new SpriteTexture(1, 1);

            _texture.SetData(new uint[1]
            {
                0xFFFF_FFFF
            });

            X = x;
            Y = y;
            Width = w;
            Height = h;
            WantUpdateSize = false;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            base.Update(totalMS, frameMS);

            _texture.Ticks = (long) totalMS;
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            if (IsDisposed)
                return false;

            if (MouseIsOver)
                return spriteBatch.Draw2D(_texture, position, new Rectangle(0 ,0 , Width, Height), RenderExtentions.GetHueVector(0, false, IsTransparent ? Alpha : 0, false));

            return base.Draw(spriteBatch, position, hue);
        }

        public override void Dispose()
        {
            base.Dispose();
            _texture?.Dispose();
        }
    }
}
