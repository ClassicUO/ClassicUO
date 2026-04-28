// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Scenes;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// Marker control that toggles a gump-level clip region. Place an instance with
    /// <see cref="DoScissor"/> = true before the children to clip, and a second instance
    /// with <see cref="DoScissor"/> = false after them to pop the clip.
    /// <para/>
    /// Previously this emitted its clip operation as a closure into both atlas and
    /// non-atlas gump streams because there were two separate flush passes. With the
    /// unified <see cref="RenderLists"/> command stream a single typed ClipPush /
    /// ClipPop entry is sufficient.
    /// </summary>
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
            if (DoScissor)
            {
                renderLists.PushClip(new Rectangle(x, y, Width, Height));
            }
            else
            {
                renderLists.PopClip();
            }

            return true;
        }
    }
}