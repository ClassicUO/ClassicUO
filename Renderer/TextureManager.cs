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
                texture.SetData(pixels);
                _staticTextures[g] = texture;
            }

            return texture;
        }

        public static Texture2D GetOrCreateLandTexture(in ushort g)
        {
            if (!_landTextures.TryGetValue(g, out var texture))
            {
                ushort[] pixels = AssetsLoader.Art.ReadLandArt(g);
                texture = new Texture2D(Device, 44, 44, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _landTextures[g] = texture;
            }

            return texture;
        }

        public static Texture2D GetOrCreateGumpTexture(in ushort g)
        {
            if (!_gumpTextures.TryGetValue(g, out var texture))
            {
                ushort[] pixels = AssetsLoader.Gumps.GetGump(g, out int w, out int h);
                texture = new Texture2D(Device, w, h, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _gumpTextures[g] = texture;
            }

            return texture;
        }

        public static Texture2D GetOrCreateTexmapTexture(in ushort g)
        {
            if (!_texmapTextures.TryGetValue(g, out var texture))
            {
                ushort[] pixels = AssetsLoader.TextmapTextures.GetTextmapTexture(g, out int size);
                texture = new Texture2D(Device, size, size, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _texmapTextures[g] = texture;
            }

            return texture;
        }


    }
}
