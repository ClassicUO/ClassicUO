using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class TextureControl : Control
    {
        public TextureControl()
        {
            CanMove = true;
            AcceptMouseInput = true;
            ScaleTexture = true;
        }

        public bool ScaleTexture { get; set; }

        public Hue Hue { get; set; }
        public bool IsPartial { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Texture != null)
                Texture.Ticks = Engine.Ticks;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hue = Vector3.Zero;
            ShaderHuesTraslator.GetHueVector(ref hue, Hue, IsPartial, Alpha);

            if (ScaleTexture)
            {
                if (Texture is ArtTexture artTexture)
                {
                    int w = Width;
                    int h = Height;

                    if (artTexture.ImageRectangle.Width < Width)
                    {
                        w = artTexture.ImageRectangle.Width;
                        x += (Width >> 1) - (w >> 1);
                    }

                    if (artTexture.ImageRectangle.Height < Height)
                    {
                        h = artTexture.ImageRectangle.Height;
                        y += (Height >> 1) - (h >> 1);
                    }


                    var r = artTexture.ImageRectangle;

                    return batcher.Draw2D(Texture, x, y, w, h, r.X, r.Y, r.Width, r.Height, ref hue);
                }

                return batcher.Draw2D(Texture, x, y, Width, Height, 0, 0, Texture.Width, Texture.Height, ref hue);
            }

            return batcher.Draw2D(Texture, x, y, ref hue);
        }
    }
}