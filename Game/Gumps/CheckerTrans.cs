using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class CheckerTrans : GumpControl
    {
        private static SpriteTexture _transparentTexture;

        public static SpriteTexture TransparentTexture
        {
            get
            {
                if (_transparentTexture == null)
                {
                    _transparentTexture = new SpriteTexture(32, 32);
                    _transparentTexture.SetData(new Color[1] { Color.Black });
                }

                _transparentTexture.Ticks = World.Ticks;
                return _transparentTexture;
            }
        }

        public CheckerTrans() : base()
        {

        }

        public CheckerTrans(in string[] parts) : this()
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            Width = int.Parse(parts[3]);
            Height = int.Parse(parts[4]);
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            spriteBatch.Draw2DTiled(_transparentTexture, new Rectangle((int)position.X, (int)position.Y, Width, Height), RenderExtentions.GetHueVector(0, false, true, false));
            return base.Draw(spriteBatch, position);
        }
    }
}
