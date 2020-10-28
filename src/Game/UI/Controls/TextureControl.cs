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

        public ushort Hue { get; set; }
        public bool IsPartial { get; set; }
        public UOTexture Texture { get; set; }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (Texture != null)
            {
                Texture.Ticks = Time.Ticks;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (Texture == null)
            {
                return false;
            }

            ResetHueVector();
            ShaderHueTranslator.GetHueVector(ref HueVector, Hue, IsPartial, Alpha);

            if (ScaleTexture)
            {
                if (Texture is ArtTexture artTexture)
                {
                    int w = Width;
                    int h = Height;
                    Rectangle r = artTexture.ImageRectangle;

                    if (r.Width < Width)
                    {
                        w = r.Width;
                        x += (Width >> 1) - (w >> 1);
                    }

                    if (r.Height < Height)
                    {
                        h = r.Height;
                        y += (Height >> 1) - (h >> 1);
                    }

                    return batcher.Draw2D(Texture, x, y, w, h, r.X, r.Y, r.Width, r.Height, ref HueVector);
                }

                return batcher.Draw2D
                    (Texture, x, y, Width, Height, 0, 0, Texture.Width, Texture.Height, ref HueVector);
            }

            return batcher.Draw2D(Texture, x, y, ref HueVector);
        }

        public override void Dispose()
        {
            Texture = null;
            base.Dispose();
        }
    }
}