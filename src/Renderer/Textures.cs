using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal static class Textures
    {
        private static readonly Dictionary<Color, Texture2D> _textures = new Dictionary<Color, Texture2D>();

        public static Texture2D GetTexture(Color color)
        {
            if (!_textures.TryGetValue(color, out var t))
            {
                t = new Texture2D(Engine.Batcher.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                t.SetData(new [] { color });
                _textures[color] = t;
            }

            return t;
        }
    }
}
