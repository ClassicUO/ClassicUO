using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal static class SolidColorTextureCache
    {
        private static readonly Dictionary<Color, Texture2D> _textures = new Dictionary<Color, Texture2D>();

        public static Texture2D GetTexture(Color color)
        {
            if (_textures.TryGetValue(color, out Texture2D texture))
            {
                return texture;
            }

            texture = new Texture2D(Client.Game.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            texture.SetData(new[] { color });
            _textures[color] = texture;

            return texture;
        }
    }
}