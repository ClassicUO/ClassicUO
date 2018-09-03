using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{

    public abstract class GumpPicBase : GumpControl
    {
        private Graphic _lastGump;

        public GumpPicBase() : base()
        {
            CanMove = true;
        }

        public Graphic Graphic { get; set; }
        public Hue Hue { get; set; }
        public bool IsPaperdoll { get; set; }


        public override void Update(in double frameMS)
        {
            if (Texture == null || Graphic != _lastGump)
            {
                _lastGump = Graphic;

                Texture = TextureManager.GetOrCreateGumpTexture(Graphic);
                Width = Texture.Width;
                Height = Texture.Height;
            }

            base.Update(frameMS);
        }
    }

    public class GumpPic : GumpPicBase
    {

        public GumpPic(in Graphic graphic) : base()
        {
            Graphic = graphic;
        }

        public GumpPic(in string[] parts) : this(Graphic.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            if (parts.Length > 4)
                Hue = Hue.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1));
        }


        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            spriteBatch.Draw2D(Texture, position, RenderExtentions.GetHueVector(Hue));
            return base.Draw(spriteBatch, position);
        }
    }
}
