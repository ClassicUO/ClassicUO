// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes.Passes
{
    /// <summary>
    /// A pass that only clears a render target without drawing anything.
    /// Used when lights are disabled and the light RT needs to be transparent.
    /// </summary>
    internal class ClearPass : RenderPass
    {
        public RenderTarget2D Target;
        public Color ClearColor;

        public ClearPass(string name) : base(name)
        {
        }

        public override void Execute(UltimaBatcher2D batcher, RenderTargets renderTargets)
        {
            var device = batcher.GraphicsDevice;
            device.SetRenderTarget(Target);
            device.Clear(ClearOptions.Target, ClearColor.ToVector4(), 0f, 0);
            device.SetRenderTarget(null);
        }
    }
}
