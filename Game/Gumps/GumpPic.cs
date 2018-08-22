using ClassicUO.Game.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{

    public abstract class GumpPicBase : GumpControl
    {
        private Graphic _lastGump;

        public GumpPicBase(in GumpControl parent) : base(parent)
        {
            // can drag
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
                Bounds = Texture.Bounds;
            }

            base.Update(frameMS);
        }
    }

    public class GumpPic : GumpPicBase
    {

        public GumpPic(in GumpControl parent, in Graphic graphic) : base(parent)
        {
            Graphic = graphic;
        }

        public GumpPic(in GumpControl parent, in string[] parts) : this(parent, Graphic.Parse(parts[3]))
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);

            if (parts.Length > 4)
                Hue = Hue.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1));
        }

        public Hue Hue { get; set; }


        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            spriteBatch.Draw2D(Texture, Bounds, RenderExtentions.GetHueVector(Hue));
            return base.Draw(spriteBatch, position);
        }
    }
}
