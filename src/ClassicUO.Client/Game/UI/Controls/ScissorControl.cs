// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.UI.Controls
{
    internal class ScissorControl : Control
    {
        public ScissorControl(bool enabled, int x, int y, int width, int height) : this(enabled)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public ScissorControl(bool enabled)
        {
            CanMove = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
            Alpha = 1.0f;
            WantUpdateSize = false;
            DoScissor = enabled;
        }

        public bool DoScissor;

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            bool clipIt(Renderer.UltimaBatcher2D batcher)
            {
                if (DoScissor)
                {
                    batcher.ClipBegin(x, y, Width, Height);
                }
                else
                {
                    batcher.ClipEnd();
                }
                return true;
            }

            renderLists.AddGumpWithAtlas(clipIt);
            renderLists.AddGumpNoAtlas(clipIt);

            return true;
        }
    }
}