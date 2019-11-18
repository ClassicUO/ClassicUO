using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class ScissorControl : Control
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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (DoScissor)
            {
                ScissorStack.PushScissors(new Rectangle(x, y, Width, Height));
                batcher.EnableScissorTest(true);
            }
            else
            {
                batcher.EnableScissorTest(false);
                ScissorStack.PopScissors();
            }

            return true;
        }
    }
}
