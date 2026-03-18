// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes.Passes
{
    internal class WorldPass : RenderPass
    {
        private readonly GameScene _scene;

        public WorldPass(GameScene scene) : base("World")
        {
            _scene = scene;
        }

        public override void Execute(UltimaBatcher2D batcher, RenderTargets renderTargets)
        {
            var device = batcher.GraphicsDevice;

            device.SetRenderTarget(renderTargets.WorldRenderTarget);
            device.Clear(ClearOptions.Target, Color.Transparent.ToVector4(), 0f, 0);

            Viewport savedViewport = device.Viewport;
            device.Viewport = _scene.Camera.GetViewport();

            Matrix matrix = _scene.Camera.ViewTransformMatrix;

            batcher.SetSampler(SamplerState.PointClamp);
            batcher.Begin(null, matrix);
            batcher.SetStencil(DepthStencilState.Default);

            _scene.DrawWorldContent(batcher);

            batcher.SetStencil(null);
            batcher.SetSampler(null);
            batcher.End();

            device.Viewport = savedViewport;
            device.SetRenderTarget(null);
        }
    }
}
