using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game
{
    class CircleOfTransparency
    {
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


        private uint[] CreateTexture(int radius, ref short width, ref short height)
        {
            _texture?.Dispose();

            int fixRadius = radius + 1;
            int mulRadius = fixRadius * 2;

            uint[] pixels = new uint[mulRadius * mulRadius];

            width = (short) mulRadius;
            height = (short) mulRadius;

            for (int x = -fixRadius; x < fixRadius; x++)
            {
                int mulX = x * x;
                int posX = ((x + fixRadius) * mulRadius) + fixRadius;

                for (int y = -fixRadius; y < fixRadius; y++)
                {
                    int r = (int) Math.Sqrt(mulX + (y * y));

                    uint pic = (uint)((r <= radius) ? ((radius - r) & 0xFF) : 0);

                    int pos = posX + y;

                    pixels[pos] = HuesHelper.RgbaToArgb(pic);
                }
            }

            return pixels;
        }

        private readonly Lazy<DepthStencilState> _stencil = new Lazy<DepthStencilState>(() =>
        {
            DepthStencilState state = new DepthStencilState();

            state.DepthBufferEnable = true;
            state.StencilEnable = true;
            state.StencilFunction = CompareFunction.Always;
            state.ReferenceStencil = 1;
            state.StencilMask = 1;

            state.StencilFail = StencilOperation.Keep;
            state.StencilDepthBufferFail = StencilOperation.Keep;
            state.StencilPass = StencilOperation.Replace;

            return state;
        });

        private static readonly Lazy<BlendState> _checkerBlend = new Lazy<BlendState>(() =>
        {
            BlendState blend = new BlendState();
            blend.ColorWriteChannels = ColorWriteChannels.Alpha;
            return blend;
        });
        public void Draw(Batcher2D batcher, int x, int y)
        {
            if (_texture != null)
            {
                X = x - Width / 2;
                Y = y - Height / 2;

                batcher.SetBlendState(_checkerBlend.Value);
                batcher.SetStencil(_stencil.Value);

                batcher.Draw2D(_texture, X, Y, new Vector3(20, 1, 0));

                batcher.SetBlendState(null);
                batcher.SetStencil(null);

            }
        }


        private static CircleOfTransparency _circle;

        public static CircleOfTransparency Circle=> _circle;

        public static CircleOfTransparency Create(int radius)
        {
            if (_circle == null)
                _circle = new CircleOfTransparency(radius);
            else
            {
                _circle._texture.Dispose();
                _circle._texture = null;
            }

            uint[] pixels = _circle.CreateTexture(radius, ref _circle._width, ref _circle._height);

            _circle.Radius = radius;

            _circle._texture = new Texture2D(Engine.Batcher.GraphicsDevice, _circle._width, _circle.Height, false, SurfaceFormat.Color);
            _circle._texture.SetData(pixels);

            return _circle;
        }
    }
}
