using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    public struct SpriteInfo
    {
        public Texture2D Texture;
        public Rectangle UV;
        public Point Center;

        public static readonly SpriteInfo Empty = new SpriteInfo { Texture = null };
    }
}
