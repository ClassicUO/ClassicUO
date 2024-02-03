#region license

// Copyright (c) 2024, andreakarasho
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

using System;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game
{
    internal static class CircleOfTransparency
    {
        private static readonly Lazy<DepthStencilState> _stencil = new Lazy<DepthStencilState>
        (
            () =>
            {
                DepthStencilState state = new DepthStencilState
                {
                    StencilEnable = true,
                    StencilFunction = CompareFunction.Always,
                    StencilPass = StencilOperation.Replace,
                    ReferenceStencil = 1
                    //DepthBufferEnable = true,
                    //DepthBufferWriteEnable = true,
                };


                return state;
            }
        );


        private static Texture2D _texture;
        private static short _width, _height;
        private static int _radius;


        public static uint[] CreateCircleTexture(int radius, ref short width, ref short height)
        {
            int fixRadius = radius + 1;
            int mulRadius = fixRadius * 2;

            uint[] pixels = new uint[mulRadius * mulRadius];

            width = (short) mulRadius;
            height = (short) mulRadius;

            for (int x = -fixRadius; x < fixRadius; x++)
            {
                int mulX = x * x;
                int posX = (x + fixRadius) * mulRadius + fixRadius;

                for (int y = -fixRadius; y < fixRadius; y++)
                {
                    int r = (int) Math.Sqrt(mulX + y * y);

                    uint pic = (uint) (r <= radius ? (radius - r) & 0xFF : 0);

                    int pos = posX + y;

                    pixels[pos] = pic;
                }
            }

            return pixels;
        }

        public static void Draw(UltimaBatcher2D batcher, Vector2 pos, ushort hue = 0)
        {
            if (_texture != null)
            {
                pos.X -= _width >> 1;
                pos.Y -= _height >> 1;

                Vector3 hueVector = new Vector3();

                if (hue == 0)
                {
                    hueVector.X = 0;
                    hueVector.Y = 0f;
                }
                else
                {
                    hueVector.X = hue;
                    hueVector.Y = 1f;
                }
               
                batcher.SetStencil(_stencil.Value);
                batcher.Draw
                (
                    _texture,
                    pos,
                    hueVector
                );
                batcher.SetStencil(null);
            }
        }

        public static void Create(int radius)
        {
            if (radius < Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS)
            {
                radius = Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS;
            }
            else if (radius > Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS)
            {
                radius = Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS;
            }

            if (_radius == radius && _texture != null && !_texture.IsDisposed)
            {
                return;
            }

            _radius = radius;
            _texture?.Dispose();
            _texture = null;

            uint[] pixels = CreateCircleTexture(radius, ref _width, ref _height);

            for (int i = 0; i < pixels.Length; i++)
            {
                ref uint pixel = ref pixels[i];

                if (pixel != 0)
                {
                    pixel = HuesHelper.RgbaToArgb(pixel);
                    //ushort value = (ushort)(pixel << 3);

                    //if (value > 0xFF)
                    //    value = 0xFF;

                    //pixel = (uint)((value << 24) | (value << 16) | (value << 8) | value);
                }
            }

            _texture = new Texture2D(Client.Game.GraphicsDevice, _width, _height);
            _texture.SetData(pixels);            
        }
    }
}