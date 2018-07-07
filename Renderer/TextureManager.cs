using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Renderer
{
    public static class TextureManager
    {
        private readonly static Dictionary<ushort, Texture2D> _staticTextures = new Dictionary<ushort, Texture2D>();
        private readonly static Dictionary<ushort, Texture2D> _landTextures = new Dictionary<ushort, Texture2D>();
        private readonly static Dictionary<ushort, Texture2D> _gumpTextures = new Dictionary<ushort, Texture2D>();
        private readonly static Dictionary<ushort, Texture2D> _texmapTextures = new Dictionary<ushort, Texture2D>();
        private readonly static Dictionary<ushort, Texture2D> _soundTextures = new Dictionary<ushort, Texture2D>();
        private readonly static Dictionary<ushort, Texture2D> _lightTextures = new Dictionary<ushort, Texture2D>();


        public static GraphicsDevice Device { get; set; }

        public static Texture2D GetOrCreateStaticTexture(in ushort g)
        {
            if (!_staticTextures.TryGetValue(g, out var texture))
            {
                ushort[] pixels = AssetsLoader.Art.ReadStaticArt(g, out short w, out short h);
                texture = new Texture2D(Device, w, h, false, SurfaceFormat.Bgra5551);
                _staticTextures[g] = texture;
            }

            return texture;
        }


    }
}
