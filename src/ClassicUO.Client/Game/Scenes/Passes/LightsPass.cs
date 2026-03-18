// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes.Passes
{
    internal class LightsPass : RenderPass
    {
        private readonly GameScene _scene;

        public Color ClearColor;

        public LightsPass(GameScene scene) : base("Lights")
        {
            _scene = scene;
        }

        public override void Execute(UltimaBatcher2D batcher, RenderTargets renderTargets)
        {
            var device = batcher.GraphicsDevice;

            device.SetRenderTarget(renderTargets.LightRenderTarget);
            device.Clear(ClearOptions.Target, ClearColor.ToVector4(), 0f, 0);

            Matrix matrix = _scene.Camera.ViewTransformMatrix;

            batcher.Begin(null, matrix);
            batcher.SetBlendState(BlendState.Additive);

            _scene.DrawLightSprites(batcher);

            batcher.SetBlendState(null);
            batcher.End();

            device.SetRenderTarget(null);
        }
    }
}
