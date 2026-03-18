// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Rendering
{
    internal class CompositePass : RenderPass
    {
        public CompositePass() : base("Composite")
        {
        }

        public override void Execute(UltimaBatcher2D batcher, RenderTargets renderTargets)
        {
            var device = batcher.GraphicsDevice;

            device.SetRenderTarget(null);
            device.Clear(ClearOptions.Target, Color.Black.ToVector4(), 0f, 0);

            renderTargets.Draw(batcher);
        }
    }
}
