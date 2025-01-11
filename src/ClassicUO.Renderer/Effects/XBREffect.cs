// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    public class XBREffect : Effect
    {
        public XBREffect(GraphicsDevice graphicsDevice) : base(graphicsDevice, Resources.GetXBRShader().ToArray())
        {
            MatrixTransform = Parameters["MatrixTransform"];
            TextureSize = Parameters["textureSize"];
        }

        public EffectParameter MatrixTransform { get; }
        public EffectParameter TextureSize { get; }
    }
}