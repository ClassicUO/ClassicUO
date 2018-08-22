using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class StaticPic : GumpControl
    {
        private readonly Graphic _graphic;

        public StaticPic(in GumpControl parent, in Graphic graphic, in Hue hue) : base(parent)
        {
            _graphic = graphic;
            Hue = hue;
        }

        public StaticPic(in GumpControl parent, in string[] parts) : this(parent, Graphic.Parse(parts[3]), parts.Length > 4 ? Hue.Parse(parts[4]) : (Hue)0)
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
        }

        public Hue Hue { get; set; }


        public override void Update(in double frameMS)
        {
            if (Texture == null)
            {
                Texture = TextureManager.GetOrCreateGumpTexture(_graphic);
                Bounds = Texture.Bounds;
            }
            base.Update(frameMS);
        }

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {

            return base.Draw(spriteBatch, position);
        }

    }
}
