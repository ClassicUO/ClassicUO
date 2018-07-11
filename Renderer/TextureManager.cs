using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Renderer
{
    public static class TextureManager
    {
        struct TextureDuration
        {
            public Texture2D Texture;
            public long Ticks;
        }

        private readonly static Dictionary<ushort, TextureDuration> _staticTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _landTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _gumpTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _texmapTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _soundTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _lightTextures = new Dictionary<ushort, TextureDuration>();


        public static GraphicsDevice Device { get; set; }

        private static long _ticks;

        public static void UpdateTicks(in long ticks)
        {
            _ticks = ticks;


            List<ushort> toremove = new List<ushort>();


            foreach (var k in _staticTextures)
            {
                if (ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _staticTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _landTextures)
            {
                if (ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _landTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _gumpTextures)
            {
                if (ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _gumpTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _texmapTextures)
            {
                if (ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _texmapTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _soundTextures)
            {
                if (ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _soundTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _lightTextures)
            {
                if (ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _lightTextures.Remove(t);
            toremove.Clear();
        }

        const long TEXTURE_TIME_LIFE = 3 * 10000000;

        public static Texture2D GetOrCreateStaticTexture(in ushort g)
        {
            if (!_staticTextures.TryGetValue(g, out var tuple))
            {
                ushort[] pixels = AssetsLoader.Art.ReadStaticArt(g, out short w, out short h);
                Texture2D texture = new Texture2D(Device, w, h, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _staticTextures[g] = tuple = new TextureDuration { Ticks = _ticks, Texture = texture };
            }
            else
                tuple.Ticks = _ticks;

            return tuple.Texture;
        }

        public static Texture2D GetOrCreateLandTexture(in ushort g)
        {
            if (!_landTextures.TryGetValue(g, out var tuple))
            {
                ushort[] pixels = AssetsLoader.Art.ReadLandArt(g);
                Texture2D texture = new Texture2D(Device, 44, 44, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _landTextures[g] = tuple = new TextureDuration { Ticks = _ticks, Texture = texture };
            }
            else
                tuple.Ticks = _ticks;

            return tuple.Texture;
        }

        public static Texture2D GetOrCreateGumpTexture(in ushort g)
        {
            if (!_gumpTextures.TryGetValue(g, out var tuple))
            {
                ushort[] pixels = AssetsLoader.Gumps.GetGump(g, out int w, out int h);
                Texture2D texture = new Texture2D(Device, w, h, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _gumpTextures[g] = tuple = new TextureDuration { Ticks = _ticks, Texture = texture };
            }
            else
                tuple.Ticks = _ticks;

            return tuple.Texture;
        }

        public static Texture2D GetOrCreateTexmapTexture(in ushort g)
        {
            if (!_texmapTextures.TryGetValue(g, out var tuple))
            {
                ushort[] pixels = AssetsLoader.TextmapTextures.GetTextmapTexture(g, out int size);
                Texture2D texture = new Texture2D(Device, size, size, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _texmapTextures[g] = tuple = new TextureDuration { Ticks = _ticks, Texture = texture };
            }
            else
                tuple.Ticks = _ticks;

            return tuple.Texture;
        }


    }
}
