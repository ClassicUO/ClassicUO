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
                DepthBufferEnable = false,
                StencilEnable = true,
                StencilFunction = CompareFunction.Always,
                ReferenceStencil = 1,
                StencilMask = 1,
                StencilFail = StencilOperation.Keep,
                StencilDepthBufferFail = StencilOperation.Keep,
                StencilPass = StencilOperation.Replace
            };


            return state;
        });

        private static readonly Lazy<BlendState> _checkerBlend = new Lazy<BlendState>(() =>
        {
            BlendState blend = BlendState.AlphaBlend;
            blend.ColorWriteChannels = ColorWriteChannels.Alpha;

            return blend;
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

        public void Draw(Batcher2D batcher, int x, int y)
        {
            if (_texture != null)
            {
                X = x - Width / 2;
                Y = y - Height / 2;
                //batcher.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, new Vector4(0, 0, 0, 1), 0, 0);

                batcher.Begin();
                batcher.SetStencil(_stencil.Value);
                //batcher.SetBlendState(_checkerBlend.Value);

                BlendState.AlphaBlend.ColorWriteChannels = ColorWriteChannels.Alpha;
                batcher.Draw2D(_texture, X, Y, Vector3.Zero);
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

            Circle.Radius = radius;

            Circle._texture = new Texture2D(Engine.Batcher.GraphicsDevice, Circle._width, Circle.Height, false, SurfaceFormat.Color);
            Circle._texture.SetData(pixels);

            return Circle;
        }
    }
}