#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ClassicUO.Game.Renderer
{
    public class SpriteTexture : Texture2D
    {
        public SpriteTexture(int width,  int height,  bool is32bit = true) : base(TextureManager.Device, width, height, false, is32bit ? SurfaceFormat.Color : SurfaceFormat.Bgra5551)
        {
        }

        public long Ticks { get; set; }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public unsafe static class TextureManager
    {
        private const long TEXTURE_TIME_LIFE = 3000;

        private static int _updateIndex;

        private static readonly Dictionary<ushort, SpriteTexture> _staticTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _landTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _gumpTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _textmapTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _lightTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<TextureAnimationFrame, SpriteTexture> _animations = new Dictionary<TextureAnimationFrame, SpriteTexture>();
        private static readonly Dictionary<RenderedText, SpriteTexture> _textTextureCache = new Dictionary<RenderedText, SpriteTexture>();



        public static GraphicsDevice Device { get; set; }

        private static long _nextGC = 5000;

        public static void Update()
        {
            IO.Resources.Animations.ClearUnusedTextures();

            var list = _textTextureCache.Where(s => World.Ticks - s.Value.Ticks >= TEXTURE_TIME_LIFE).ToList();

            foreach (var t in list)
            {
                t.Value.Dispose();
                _textTextureCache.Remove(t.Key);
            }

            CheckSpriteTexture(_staticTextureCache);
            CheckSpriteTexture(_landTextureCache);
            CheckSpriteTexture(_gumpTextureCache);
            CheckSpriteTexture(_textmapTextureCache);
            CheckSpriteTexture(_lightTextureCache);
            World.Map?.ClearUnusedBlocks();


            //if (World.Ticks - _nextGC >= 5000)
            //{

            //    Utility.Service.Get<Log>().Message(Utility.LogTypes.Info, "GARBAGE!");
            //    GC.AddMemoryPressure(sizeOfField);
            //    GC.RemoveMemoryPressure(sizeOfField);
            //    GC.Collect();
            //    //GC.WaitForPendingFinalizers();
            //    _nextGC = World.Ticks;
            //}

            //if (_updateIndex == 0)
            //{
            //    //var list = _animations.Where(s => World.Ticks - s.Value.Ticks >= TEXTURE_TIME_LIFE).ToList();

            //    //foreach (var t in list)
            //    //{
            //    //    t.Value.Dispose();

            //    //    //Array.Clear(t.Key.Pixels, 0, t.Key.Pixels.Length);
            //    //    //t.Key.Pixels = null;
            //    //    //t.Key.Width = 0;
            //    //    //t.Key.Height = 0;

            //    //    _animations.Remove(t.Key);

            //    //}


            //    _updateIndex++;
            //}
            //else if (_updateIndex == 1)
            //{

            //    _updateIndex++;
            //}
            //else
            //{


            //    if (_updateIndex == 2)
            //    {
            //        CheckSpriteTexture(_staticTextureCache);
            //    }
            //    else if (_updateIndex == 3)
            //    {
            //        CheckSpriteTexture(_landTextureCache);
            //    }
            //    else if (_updateIndex == 4)
            //    {
            //        CheckSpriteTexture(_gumpTextureCache);
            //    }
            //    else if (_updateIndex == 5)
            //    {
            //        CheckSpriteTexture(_textmapTextureCache);
            //    }
            //    else if (_updateIndex == 6)
            //    {
            //        CheckSpriteTexture(_lightTextureCache);
            //    }
            //    else if (_updateIndex == 7)
            //    {
            //        World.Map?.ClearUnusedBlocks();
            //    }
            //    else
            //    {
            //        _updateIndex = 0;
            //    }
            //}
        }

        private static void CheckSpriteTexture(Dictionary<ushort, SpriteTexture> dict)
        {

            List<KeyValuePair<ushort, SpriteTexture>> list = new List<KeyValuePair<ushort, SpriteTexture>>();

            foreach (var t in dict)
            {
                if (World.Ticks - t.Value.Ticks >= TEXTURE_TIME_LIFE)
                {
                    list.Add(t);
                }
            }

            //var toremove = dict.Where(s => World.Ticks - s.Value.Ticks >= TEXTURE_TIME_LIFE).ToList();
            foreach (var t in list)
            {
                dict[t.Key].Dispose();
                dict.Remove(t.Key);
            }

            _updateIndex++;
        }

        public static SpriteTexture GetOrCreateAnimTexture(TextureAnimationFrame frame)
        {
            if (!_animations.TryGetValue(frame, out var sprite))
            {
                //sprite = new SpriteTexture(frame.Width, frame.Height, false) { Ticks = World.Ticks };
                //sprite.SetData(frame.Pixels);
                _animations[frame] = sprite;
            }
            else
            {
                sprite.Ticks = World.Ticks;
            }

            return sprite;
        }


        public static SpriteTexture GetOrCreateStaticTexture(ushort g)
        {
            if (!_staticTextureCache.TryGetValue(g, out var texture) || texture.IsDisposed)
            {
                var pixels = Art.ReadStaticArt(g, out short w, out short h);

                texture = new SpriteTexture(w, h, false) { Ticks = World.Ticks };

                fixed(ushort* ptr = pixels)
                    texture.SetDataPointerEXT(0, texture.Bounds, (IntPtr)ptr, pixels.Length);
 
                //texture.SetData(pixels);
                _staticTextureCache[g] = texture;
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateLandTexture(ushort g)
        {
            if (!_landTextureCache.TryGetValue(g, out var texture) || texture.IsDisposed)
            {
                var pixels = Art.ReadLandArt(g);
                texture = new SpriteTexture(44, 44, false) { Ticks = World.Ticks };
                fixed (ushort* ptr = pixels)
                    texture.SetDataPointerEXT(0, texture.Bounds, (IntPtr)ptr, pixels.Length);
                //texture.SetData(pixels);
                _landTextureCache[g] = texture;
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateGumpTexture(ushort g)
        {
            if (!_gumpTextureCache.TryGetValue(g, out var texture) || texture.IsDisposed)
            {
                var pixels = IO.Resources.Gumps.GetGump(g, out int w, out int h);
                texture = new SpriteTexture(w, h, false) { Ticks = World.Ticks };
                fixed (ushort* ptr = pixels)
                    texture.SetDataPointerEXT(0, texture.Bounds, (IntPtr)ptr, pixels.Length);
                //texture.SetData(pixels);
                _gumpTextureCache[g] = texture;
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateTexmapTexture(ushort g)
        {
            if (!_textmapTextureCache.TryGetValue(g, out var texture) || texture.IsDisposed)
            {
                var pixels = TextmapTextures.GetTextmapTexture(g, out int size);
                texture = new SpriteTexture(size, size, false) { Ticks = World.Ticks };
                fixed (ushort* ptr = pixels)
                    texture.SetDataPointerEXT(0, texture.Bounds, (IntPtr)ptr, pixels.Length);
                _textmapTextureCache[g] = texture;
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateStringTextTexture(RenderedText gt)
        {
            if (!_textTextureCache.TryGetValue(gt, out var texture) || texture.IsDisposed)
            {
                //uint[] data;
                //int linesCount;

                //if (gt.IsHTML)
                //    Fonts.SetUseHTML(true);

                //if (gt.IsUnicode)
                //{
                //    (data, gt.Width, gt.Height, linesCount, gt.Links) = Fonts.GenerateUnicode(gt.Font, gt.Text, gt.Hue, gt.Cell, gt.MaxWidth, gt.Align, (ushort)gt.FontStyle);
                //}
                //else
                //{
                //    (data, gt.Width, gt.Height, linesCount, gt.IsPartialHue) = Fonts.GenerateASCII(gt.Font, gt.Text, gt.Hue, gt.MaxWidth, gt.Align, (ushort)gt.FontStyle);
                //}

                //texture = new SpriteTexture(gt.Width, gt.Height);
                //texture.SetData(data);
                //_textTextureCache[gt] = texture;


                //if (gt.IsHTML)
                //    Fonts.SetUseHTML(false);
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }
    }
}