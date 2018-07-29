using ClassicUO.AssetsLoader;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Renderer
{
    public class SpriteTexture : Texture2D
    {      
        public SpriteTexture(in int width, in int height, bool is32bit = true) : base(TextureManager.Device, width,
            height, false, is32bit ? SurfaceFormat.Color : SurfaceFormat.Bgra5551)
        {
            ID = TextureManager.NextID;
        }

        public long Ticks { get; set; }
        public int ID { get; }
    }

    public static class TextureManager
    {
        private const long TEXTURE_TIME_LIFE = 3000;

        private static int _updateIndex;

        private static readonly Dictionary<ushort, SpriteTexture[][][]> _animTextureCache = new Dictionary<ushort, SpriteTexture[][][]>();
        //private static readonly SpriteTexture[] _staticTextureCache = new SpriteTexture[Art.ART_COUNT];
        //private static readonly SpriteTexture[] _landTextureCache = new SpriteTexture[Art.ART_COUNT];
        //private static readonly SpriteTexture[] _gumpTextureCache = new SpriteTexture[Gumps.GUMP_COUNT];
        //private static readonly SpriteTexture[] _textmapTextureCache = new SpriteTexture[TextmapTextures.TEXTMAP_COUNT];
        ////private static readonly SpriteTexture[] _soundTextureCache = new SpriteTexture[]
        //private static readonly SpriteTexture[] _lightTextureCache = new SpriteTexture[Light.LIGHT_COUNT];


        private static readonly Dictionary<ushort, SpriteTexture> _staticTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _landTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _gumpTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _textmapTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _lightTextureCache = new Dictionary<ushort, SpriteTexture>();


        private static readonly Dictionary<AnimationFrame, SpriteTexture> _animations = new Dictionary<AnimationFrame, SpriteTexture>();


        public static GraphicsDevice Device { get; set; }


        private static int _first = 0;

        public static int NextID
        {
            get
            {
                return _first++;
            }
        }


        public static void Update()
        {

            if (_updateIndex == 0)
            {
                List<AnimationFrame> toremove = new List<AnimationFrame>();

                foreach (var k in _animations)
                {
                    if (World.Ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    {
                        k.Value.Dispose();
                        toremove.Add(k.Key);
                    }
                }

                //foreach (var k in _animTextureCache)
                //{
                //    bool rem = true;
                //    for (int group = 0; group < 100; group++)
                //    {
                //        var sprites = k.Value;

                //        if (sprites[group] != null)
                //        {
                //            for (int dir = 0; dir < 5; dir++)
                //            {
                //                if (sprites[group][dir] != null)
                //                {
                //                    for (int i = 0; i < 25; i++)
                //                    {
                //                        var texture = sprites[group][dir][i];
                //                        if (texture != null)
                //                        {
                //                            if (World.Ticks - texture.Ticks >= TEXTURE_TIME_LIFE)
                //                            {
                //                                texture.Dispose();
                //                                texture = null;                                           
                //                            }
                //                            else if (rem)
                //                                rem = false;
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }

                //    if (rem)
                //        toremove.Add(k.Key);
                //}

                foreach (var t in toremove)
                    _animations.Remove(t);

                _updateIndex++;
            }
            else
            {       
                void check(in Dictionary<ushort, SpriteTexture> dict)
                {
                    var toremove = dict.Where(s => World.Ticks - s.Value.Ticks >= TEXTURE_TIME_LIFE).ToList();
                    foreach (var t in toremove)
                    {
                        dict[t.Key].Dispose();
                        dict[t.Key] = null;
                        dict.Remove(t.Key);
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
                    check(_textmapTextureCache);
                else if (_updateIndex == 5)
                    check(_lightTextureCache);
                else
                    _updateIndex = 0;
            }

            //if (_updateIndex == 0)
            //{
            //    for (int g = 0; g < _animTextureCache.Length; g++)
            //    for (int group = 0; group < _animTextureCache[g]?.Length; group++)
            //    for (int dir = 0; dir < _animTextureCache[g][group]?.Length; dir++)
            //    for (int idx = 0; idx < _animTextureCache[g][group][dir]?.Length; idx++)
            //        if (World.Ticks - _animTextureCache[g][group][dir][idx]?.Ticks >= TEXTURE_TIME_LIFE)
            //        {
            //            _animTextureCache[g][group][dir][idx].Dispose();
            //            _animTextureCache[g][group][dir][idx] = null;
            //        }

            //    _updateIndex++;
            //}
            //else if (_updateIndex > 0)
            //{
            //    void check(in SpriteTexture[] array)
            //    {
            //        for (int i = 0; i < array.Length; i++)
            //            if (World.Ticks - array[i]?.Ticks >= TEXTURE_TIME_LIFE)
            //            {
            //                array[i].Dispose();
            //                array[i] = null;
            //            }

            //        _updateIndex++;
            //    }

            //    if (_updateIndex == 1)
            //        check(_staticTextureCache);
            //    else if (_updateIndex == 2)
            //        check(_landTextureCache);
            //    else if (_updateIndex == 3)
            //        check(_gumpTextureCache);
            //    else if (_updateIndex == 4)
            //        check(_lightTextureCache);
            //    else if (_updateIndex == 5)
            //        check(_textmapTextureCache);
            //    else
            //        _updateIndex = 0;
            //}
        }

        public static SpriteTexture GetOrCreateAnimTexture(in AnimationFrame frame)
        {
            if (!_animations.TryGetValue(frame, out var sprite))
            {
                sprite = new SpriteTexture(frame.Width, frame.Heigth, false)
                {
                    Ticks = World.Ticks
                };
                sprite.SetData(frame.Pixels);
                _animations[frame] = sprite;
            }
            else
                sprite.Ticks = World.Ticks;

            return sprite;
        }

        public static SpriteTexture GetOrCreateAnimTexture(in ushort g, in byte group, in byte dir, in int index,
            in AnimationFrame[] frames)
        {

            if (!_animTextureCache.TryGetValue(g, out var sprites) || sprites[group] == null || sprites[group][dir] == null 
                || sprites[group][dir][index] == null)
            {

                if (sprites == null)
                {
                    sprites = new SpriteTexture[100][][];
                    _animTextureCache[g] = sprites;
                }
                if (sprites[group] == null)
                    sprites[group] = new SpriteTexture[5][];
                if (sprites[group][dir] == null)
                    sprites[group][dir] = new SpriteTexture[25];

                if (sprites[group][dir][index] == null)
                {
                    SpriteTexture texture = new SpriteTexture(frames[index].Width, frames[index].Heigth, false)
                    {
                        Ticks = World.Ticks
                    };

                    texture.SetData(frames[index].Pixels);
                    sprites[group][dir][index] = texture;
                    
                }
            }

            sprites[group][dir][index].Ticks = World.Ticks;
            return sprites[group][dir][index];
        }

        public static SpriteTexture GetOrCreateStaticTexture(in ushort g)
        {
            if (!_staticTextureCache.TryGetValue(g, out var texture))
            {
                ushort[] pixels = Art.ReadStaticArt(g, out short w, out short h);

                texture = new SpriteTexture(w, h, false)
                {
                    Ticks = World.Ticks
                };

                texture.SetData(pixels);
                _staticTextureCache[g] = texture;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateLandTexture(in ushort g)
        {
            if (!_landTextureCache.TryGetValue(g, out var texture))
            {
                ushort[] pixels = Art.ReadLandArt(g);
                texture = new SpriteTexture(44, 44, false)
                {
                    Ticks = World.Ticks
                };
                texture.SetData(pixels);
                _landTextureCache[g] = texture;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateGumpTexture(in ushort g)
        {
            if (!_gumpTextureCache.TryGetValue(g, out var texture))
            {
                ushort[] pixels = Gumps.GetGump(g, out int w, out int h);
                texture = new SpriteTexture(w, h, false)
                {
                    Ticks = World.Ticks
                };
                texture.SetData(pixels);
                _gumpTextureCache[g] = texture;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateTexmapTexture(in ushort g)
        {
            if (!_textmapTextureCache.TryGetValue(g, out var texture))
            {
                ushort[] pixels = TextmapTextures.GetTextmapTexture(g, out int size);
                texture = new SpriteTexture(size, size, false)
                {
                    Ticks = World.Ticks
                };
                texture.SetData(pixels);
                _textmapTextureCache[g] = texture;
            }

            return texture;
        }
    }
}