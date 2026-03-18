// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Rendering
{
    internal class UIPass : RenderPass
    {
        private readonly GameController _controller;

        public UIPass(GameController controller) : base("UI")
        {
            _controller = controller;
        }

        public override void Execute(UltimaBatcher2D batcher, RenderTargets renderTargets)
        {
            var device = batcher.GraphicsDevice;

            device.SetRenderTarget(renderTargets.UiRenderTarget);
            device.Clear(ClearOptions.Target, Color.Transparent.ToVector4(), 0f, 0);

            if ((_controller.UO.World?.InGame ?? false) && SelectedObject.Object is TextObject t)
            {
                if (t.IsTextGump)
                {
                    t.ToTopD();
                }
                else
                {
                    _controller.UO.World.WorldTextManager?.MoveToTop(t);
                }
            }

            SelectedObject.HealthbarObject = null;
            SelectedObject.SelectedContainer = null;

            var scene = _controller.Scene;

            batcher.Begin();
            if (scene != null && scene.IsLoaded && !scene.IsDestroyed)
            {
                scene.DrawUI(batcher);
            }
            batcher.End();

            _controller.UI.Draw(batcher);

            batcher.Begin();
            _controller.UO.GameCursor?.Draw(batcher);
            batcher.End();

            device.SetRenderTarget(null);
        }
    }
}
