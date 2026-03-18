// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes.Passes
{
    internal class DeathScreenPass : RenderPass
    {
        private readonly GameScene _scene;

        public DeathScreenPass(GameScene scene) : base("DeathScreen")
        {
            _scene = scene;
        }

        public override void Execute(UltimaBatcher2D batcher, RenderTargets renderTargets)
        {
            var device = batcher.GraphicsDevice;

            device.SetRenderTarget(null);
            device.Clear(ClearOptions.Target, Color.Black.ToVector4(), 0f, 0);

            batcher.Begin();
            _scene.DrawDeathScreen(batcher);
            batcher.End();
        }
    }
}
