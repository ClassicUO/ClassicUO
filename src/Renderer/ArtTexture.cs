using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer
{
    internal class ArtTexture : UOTexture
    {
        public ArtTexture(int width, int height)
            : base(width, height)
        {
        }

        public Rectangle ImageRectangle;
    }
}