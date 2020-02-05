#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System;

using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game
{
    static class CircleOfTransparency
    {
        private static readonly Lazy<DepthStencilState> _stencil = new Lazy<DepthStencilState>(() =>
        {
            DepthStencilState state = new DepthStencilState
            {
                StencilEnable = true,
                StencilFunction = CompareFunction.Always,
                StencilPass = StencilOperation.Replace,
                ReferenceStencil = 1,
                //DepthBufferEnable = true,
                //DepthBufferWriteEnable = true,
            };


            return state;
        });


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

        private static Vector3 _hueVector;

        public static void Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (_texture != null)
            {
                x -= (_width >> 1);
                y -= (_height >> 1);

                //batcher.Begin();
                batcher.SetStencil(_stencil.Value);
                batcher.Draw2D(_texture, x, y, ref _hueVector);
                batcher.SetStencil(null);
                //batcher.End();
            }
        }

        public static void Create(int radius)
        {
            if (radius < Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS)
                radius = Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS;
            else if (radius > Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS)
                radius = Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS;

            if (_radius == radius && _texture != null && !_texture.IsDisposed)
                return;

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