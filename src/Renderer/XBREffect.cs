using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    class XBREffect : MatrixEffect
    {
        private readonly EffectParameter _textureSizeParam;
        private Vector2 _vectorSize;

        public XBREffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, ClassicUO.Renderer.Resources.xBREffect)
        {
            _textureSizeParam = Parameters["textureSize"];
        }

        public void SetSize(float w, float h)
        {
            _vectorSize.X = w;
            _vectorSize.Y = h;

            _textureSizeParam.SetValue(_vectorSize);
        }
    }
}
