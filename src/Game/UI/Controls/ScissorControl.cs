using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    class ScissorControl : Control
    {
        public ScissorControl(int x, int y, int width, int height)
        {
            CanMove = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
            Alpha = 1.0f;
            WantUpdateSize = false;

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }
    }
}
