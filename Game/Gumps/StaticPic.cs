using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class StaticPic : GumpControl
    {
        private readonly Graphic _graphic;

        public StaticPic(Graphic graphic,  Hue hue) : base()
        {
            _graphic = graphic;
            Hue = hue;

            CanMove = true;
        }

        public StaticPic(GumpControl parent,  string[] parts) : this(Graphic.Parse(parts[3]), parts.Length > 4 ? Hue.Parse(parts[4]) : (Hue)0)
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
        }

        public Hue Hue { get; set; }


        public override void Update(double frameMS)
        {
            if (Texture == null || Texture.IsDisposed)
            {
                Texture = TextureManager.GetOrCreateGumpTexture(_graphic);
                Width = Texture.Width;
                Height = Texture.Height;
            }
            base.Update(frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch,  Vector3 position)
        {
            spriteBatch.Draw2D(Texture, position, RenderExtentions.GetHueVector(Hue, false, false, true));
            return base.Draw(spriteBatch, position);
        }

    }
}
