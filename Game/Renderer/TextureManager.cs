using ClassicUO.AssetsLoader;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Renderer
{
    public class TextureDuration : Texture2D
    {
        public long Ticks;

        public TextureDuration(in int width, in int height, bool is32bit = true) : base(TextureManager.Device, width,
            height, false, is32bit ? SurfaceFormat.Color : SurfaceFormat.Bgra5551)
        {
        }
    }

    public static class TextureManager
    {
        private const long TEXTURE_TIME_LIFE = 3000;

        private static int _updateIndex;

        private static readonly TextureDuration[][][][] _animTextureCache =
            new TextureDuration[Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT][][][];

        private static readonly TextureDuration[] _staticTextureCache = new TextureDuration[Art.ART_COUNT];
        private static readonly TextureDuration[] _landTextureCache = new TextureDuration[Art.ART_COUNT];
        private static readonly TextureDuration[] _gumpTextureCache = new TextureDuration[Gumps.GUMP_COUNT];

        private static readonly TextureDuration[] _textmapTextureCache =
            new TextureDuration[TextmapTextures.TEXTMAP_COUNT];

        //private static readonly TextureDuration[] _soundTextureCache = new TextureDuration[]
        private static readonly TextureDuration[] _lightTextureCache = new TextureDuration[Light.LIGHT_COUNT];


        public static GraphicsDevice Device { get; set; }


        public static void Update()
        {
            if (_updateIndex == 0)
            {
                for (var g = 0; g < _animTextureCache.Length; g++)
                for (var group = 0; group < _animTextureCache[g]?.Length; group++)
                for (var dir = 0; dir < _animTextureCache[g][group]?.Length; dir++)
                for (var idx = 0; idx < _animTextureCache[g][group][dir]?.Length; idx++)
                    if (World.Ticks - _animTextureCache[g][group][dir][idx]?.Ticks >= TEXTURE_TIME_LIFE)
                    {
                        _animTextureCache[g][group][dir][idx].Dispose();
                        _animTextureCache[g][group][dir][idx] = null;
                    }

                _updateIndex++;
            }
            else if (_updateIndex > 0)
            {
                void check(in TextureDuration[] array)
                {
                    for (var i = 0; i < array.Length; i++)
                        if (World.Ticks - array[i]?.Ticks >= TEXTURE_TIME_LIFE)
                        {
                            array[i].Dispose();
                            array[i] = null;
                        }

                    _updateIndex++;
                }

                if (_updateIndex == 1)
                    check(_staticTextureCache);
                else if (_updateIndex == 2)
                    check(_landTextureCache);
                else if (_updateIndex == 3)
                    check(_gumpTextureCache);
                else if (_updateIndex == 4)
                    check(_lightTextureCache);
                else if (_updateIndex == 5)
                    check(_textmapTextureCache);
                else
                    _updateIndex = 0;
            }
        }


        public static ref TextureDuration GetOrCreateAnimTexture(in ushort g, in byte group, in byte dir, in int index,
            in AnimationFrame[] frames)
        {
            if (_animTextureCache[g] == null)
                _animTextureCache[g] = new TextureDuration[100][][];
            if (_animTextureCache[g][group] == null)
                _animTextureCache[g][group] = new TextureDuration[5][];
            if (_animTextureCache[g][group][dir] == null)
                _animTextureCache[g][group][dir] = new TextureDuration[50];

            if (_animTextureCache[g][group][dir][index] == null)
                for (var i = 0; i < frames.Length; i++)
                {
                    if (frames[i].Width <= 0 || frames[i].Heigth <= 0)
                        continue;

                    var texture = new TextureDuration(frames[i].Width, frames[i].Heigth, false)
                    {
                        Ticks = World.Ticks
                    };

                    texture.SetData(frames[i].Pixels);
                    _animTextureCache[g][group][dir][i] = texture;
                }

            _animTextureCache[g][group][dir][index].Ticks = World.Ticks;
            return ref _animTextureCache[g][group][dir][index];
        }

        public static ref TextureDuration GetOrCreateStaticTexture(in ushort g)
        {
            if (_staticTextureCache[g] == null)
            {
                var pixels = Art.ReadStaticArt(g, out var w, out var h);

                var texture = _staticTextureCache[g] ?? new TextureDuration(w, h, false)
                {
                    Ticks = World.Ticks
                };

                texture.SetData(pixels);
                _staticTextureCache[g] = texture;
            }

            return ref _staticTextureCache[g];
        }

        public static ref TextureDuration GetOrCreateLandTexture(in ushort g)
        {
            if (_landTextureCache[g] == null)
            {
                var pixels = Art.ReadLandArt(g);
                var texture = new TextureDuration(44, 44, false)
                {
                    Ticks = World.Ticks
                };
                texture.SetData(pixels);
                _landTextureCache[g] = texture;
            }

            return ref _landTextureCache[g];
        }

        public static ref TextureDuration GetOrCreateGumpTexture(in ushort g)
        {
            if (_gumpTextureCache[g] == null)
            {
                var pixels = Gumps.GetGump(g, out var w, out var h);
                var texture = new TextureDuration(w, h, false)
                {
                    Ticks = World.Ticks
                };
                texture.SetData(pixels);
                _gumpTextureCache[g] = texture;
            }

            return ref _gumpTextureCache[g];
        }

        public static ref TextureDuration GetOrCreateTexmapTexture(in ushort g)
        {
            if (_textmapTextureCache[g] == null)
            {
                var pixels = TextmapTextures.GetTextmapTexture(g, out var size);
                var texture = new TextureDuration(size, size, false)
                {
                    Ticks = World.Ticks
                };
                texture.SetData(pixels);
                _textmapTextureCache[g] = texture;
            }

            return ref _textmapTextureCache[g];
        }
    }
}