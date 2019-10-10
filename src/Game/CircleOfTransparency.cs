#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game
{
    internal class CircleOfTransparency
    {
        private static readonly Lazy<DepthStencilState> _stencil = new Lazy<DepthStencilState>(() =>
        {
            DepthStencilState state = new DepthStencilState
            {
                StencilEnable = true,
                StencilFunction = CompareFunction.Always,
                StencilPass = StencilOperation.Replace,
                ReferenceStencil = 1,
                DepthBufferEnable = false,
            };


            return state;
        });


        private Texture2D _texture;
        private short _width, _height;


        private CircleOfTransparency(int radius)
        {
            Radius = radius;
        }

        public int Radius { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public short Width => _width;
        public short Height => _height;

        public static CircleOfTransparency Circle { get; private set; }


        public static uint[] CreateTexture(int radius, ref short width, ref short height)
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

        public void Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (_texture != null)
            {
                X = x - (Width >> 1);
                Y = y - (Height >> 1);

                batcher.Begin();
                batcher.SetStencil(_stencil.Value);
                //batcher.SetBlendState(_checkerBlend.Value);

                //_hueVector.X = 23;
                //_hueVector.Y = 1;
                //_hueVector.Z = 0;

                BlendState.AlphaBlend.ColorWriteChannels = ColorWriteChannels.Alpha;
                batcher.Draw2D(_texture, X, Y, ref _hueVector);
                BlendState.AlphaBlend.ColorWriteChannels = ColorWriteChannels.All;


                //batcher.SetBlendState(null);
                batcher.SetStencil(null);

                batcher.End();
            }
        }

        public static CircleOfTransparency Create(int radius)
        {
            if (Circle == null)
                Circle = new CircleOfTransparency(radius);
            else
            {
                Circle._texture.Dispose();
                Circle._texture = null;
            }

            uint[] pixels = CreateTexture(radius, ref Circle._width, ref Circle._height);

            for (int i = 0; i < pixels.Length; i++)
            {
                ref uint pixel = ref pixels[i];

                if (pixel != 0)
                {
                    pixel = Color.Black.PackedValue;
                    //ushort value = (ushort)(pixel << 3);

                    //if (value > 0xFF)
                    //    value = 0xFF;

                    //pixel = (uint)((value << 24) | (value << 16) | (value << 8) | value);
                }
            }


            Circle.Radius = radius;

            Circle._texture = new Texture2D(Engine.Batcher.GraphicsDevice, Circle._width, Circle.Height);
            Circle._texture.SetData(pixels);

            return Circle;
        }
    }
}