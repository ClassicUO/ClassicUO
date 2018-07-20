using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Renderer
{
    class TextureDuration
    {
        public Texture2D Texture;
        public long Ticks;
    }

    public static class TextureManager
    {

        const long TEXTURE_TIME_LIFE = 3000;


       


        private readonly static Dictionary<ushort, TextureDuration> _staticTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _landTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _gumpTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _texmapTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _soundTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration> _lightTextures = new Dictionary<ushort, TextureDuration>();
        private readonly static Dictionary<ushort, TextureDuration[][][]> _animTextures = new Dictionary<ushort, TextureDuration[][][]>();


        public static GraphicsDevice Device { get; set; }


        public static void UpdateTicks()
        {

            List<ushort> toremove = new List<ushort>();

            //foreach (var k in _animTextures)
            //{
            //    bool all = true;
            //    foreach (var j in k.Value)
            //    {
            //        if (Game.World.Ticks - j.Ticks < TEXTURE_TIME_LIFE)
            //        {
            //            all = false;
            //            break;
            //        }
            //    }

            //    if (all)
            //        toremove.Add(k.Key);
            //}
            //foreach (var t in toremove)
            //    _animTextures.Remove(t);
            //toremove.Clear();

            foreach (var k in _staticTextures)
            {
                if (Game.World.Ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _staticTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _landTextures)
            {
                if (Game.World.Ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _landTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _gumpTextures)
            {
                if (Game.World.Ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _gumpTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _texmapTextures)
            {
                if (Game.World.Ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _texmapTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _soundTextures)
            {
                if (Game.World.Ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _soundTextures.Remove(t);
            toremove.Clear();


            foreach (var k in _lightTextures)
            {
                if (Game.World.Ticks - k.Value.Ticks >= TEXTURE_TIME_LIFE)
                    toremove.Add(k.Key);
            }

            foreach (var t in toremove)
                _lightTextures.Remove(t);
            toremove.Clear();
        }


        public static Texture2D GetOrCreateAnimTexture(in ushort g, in byte group, in byte dir, in int index)
        {
            if (!_animTextures.TryGetValue(g, out var array) || array[group] == null || array[group][dir] == null || array[group][dir][index] == null)
            {
                var frames = AssetsLoader.Animations.GetAnimationFrames(g, group, dir);

                if (array == null)
                    array = new TextureDuration[100][][];
                if (array[group] == null)
                    array[group] = new TextureDuration[5][];
                if (array[group][dir] == null)
                    array[group][dir] = new TextureDuration[50];
                if (array[group][dir][index] == null)
                    array[group][dir][index] = new TextureDuration();


                for (int i = 0; i < frames.Length; i++)
                {
                    Texture2D texture = new Texture2D(Device, frames[i].Width, frames[i].Heigth, false, SurfaceFormat.Bgra5551);
                    texture.SetData(frames[i].Pixels);

                    array[group][dir][i] = new TextureDuration()
                    {
                        Texture = texture,
                        Ticks = Game.World.Ticks
                    };
                }

                _animTextures[g] = array;

            }
            else
                array[group][dir][index].Ticks = Game.World.Ticks;

            return array[group][dir][index].Texture;
        }

        public static Texture2D GetOrCreateStaticTexture(in ushort g)
        {
            if (!_staticTextures.TryGetValue(g, out var tuple))
            {
                ushort[] pixels = AssetsLoader.Art.ReadStaticArt(g, out short w, out short h);
                Texture2D texture = new Texture2D(Device, w, h, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _staticTextures[g] = tuple = new TextureDuration { Ticks = Game.World.Ticks, Texture = texture };
            }
            else
                tuple.Ticks = Game.World.Ticks;

            return tuple.Texture;
        }

        public static Texture2D GetOrCreateLandTexture(in ushort g)
        {
            if (!_landTextures.TryGetValue(g, out var tuple))
            {
                ushort[] pixels = AssetsLoader.Art.ReadLandArt(g);
                Texture2D texture = new Texture2D(Device, 44, 44, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _landTextures[g] = tuple = new TextureDuration { Ticks = Game.World.Ticks, Texture = texture };
            }
            else
                tuple.Ticks = Game.World.Ticks;

            return tuple.Texture;
        }

        public static Texture2D GetOrCreateGumpTexture(in ushort g)
        {
            if (!_gumpTextures.TryGetValue(g, out var tuple))
            {
                ushort[] pixels = AssetsLoader.Gumps.GetGump(g, out int w, out int h);
                Texture2D texture = new Texture2D(Device, w, h, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _gumpTextures[g] = tuple = new TextureDuration { Ticks = Game.World.Ticks, Texture = texture };
            }
            else
                tuple.Ticks = Game.World.Ticks;

            return tuple.Texture;
        }

        public static Texture2D GetOrCreateTexmapTexture(in ushort g)
        {
            if (!_texmapTextures.TryGetValue(g, out var tuple))
            {
                ushort[] pixels = AssetsLoader.TextmapTextures.GetTextmapTexture(g, out int size);
                Texture2D texture = new Texture2D(Device, size, size, false, SurfaceFormat.Bgra5551);
                texture.SetData(pixels);
                _texmapTextures[g] = tuple = new TextureDuration { Ticks = Game.World.Ticks, Texture = texture };
            }
            else
                tuple.Ticks = Game.World.Ticks;

            return tuple.Texture;
        }


    }
}
