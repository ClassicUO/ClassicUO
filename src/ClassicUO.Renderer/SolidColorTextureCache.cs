#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    public static class SolidColorTextureCache
    {
        private static readonly Dictionary<Color, Texture2D> _textures = new Dictionary<Color, Texture2D>();

        private static GraphicsDevice _device;

        private static readonly Color[] _commonColors = new[]
        {
            Color.Black, Color.White, Color.Gray, Color.Transparent,
            new Color(128, 128, 128), new Color(255, 255, 255, 128),
            new Color(0, 0, 0, 128), new Color(180, 50, 50), new Color(80, 15, 15)
        };

        public static void Initialize(GraphicsDevice device)
        {
            _device = device;
            _textures.Clear();
            _roundedTextures.Clear();
            foreach (Color c in _commonColors)
            {
                GetTexture(c);
            }
        }

        public static Texture2D GetTexture(Color color)
        {
            if (_textures.TryGetValue(color, out Texture2D texture) && texture != null && !texture.IsDisposed)
            {
                return texture;
            }

            if (texture != null && texture.IsDisposed)
            {
                _textures.Remove(color);
            }

            texture = new Texture2D
            (
                _device,
                1,
                1,
                false,
                SurfaceFormat.Color
            );

            texture.SetData(new[] { color });
            _textures[color] = texture;

            return texture;
        }

        private static readonly Dictionary<(int, int, int, int), Texture2D> _roundedTextures = new Dictionary<(int, int, int, int), Texture2D>();

        public static Texture2D GetRoundedRectTexture(int width, int height, int radius, Color color)
        {
            int r = Math.Min(radius, Math.Min(width, height) / 2);
            var key = (width, height, r, (int)color.PackedValue);
            if (_roundedTextures.TryGetValue(key, out Texture2D texture) && texture != null && !texture.IsDisposed)
            {
                return texture;
            }

            if (texture != null && texture.IsDisposed)
                _roundedTextures.Remove(key);

            var pixels = new Color[width * height];
            int rSq = r * r;

            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    bool inside = false;
                    if (px >= r && px < width - r && py >= r && py < height - r)
                        inside = true;
                    else if (px < r && py < r)
                        inside = (px - r) * (px - r) + (py - r) * (py - r) <= rSq;
                    else if (px >= width - r && py < r)
                        inside = (px - (width - 1 - r)) * (px - (width - 1 - r)) + (py - r) * (py - r) <= rSq;
                    else if (px < r && py >= height - r)
                        inside = (px - r) * (px - r) + (py - (height - 1 - r)) * (py - (height - 1 - r)) <= rSq;
                    else if (px >= width - r && py >= height - r)
                        inside = (px - (width - 1 - r)) * (px - (width - 1 - r)) + (py - (height - 1 - r)) * (py - (height - 1 - r)) <= rSq;

                    pixels[py * width + px] = inside ? color : Color.Transparent;
                }
            }

            texture = new Texture2D(_device, width, height, false, SurfaceFormat.Color);
            texture.SetData(pixels);
            _roundedTextures[key] = texture;

            return texture;
        }
    }
}