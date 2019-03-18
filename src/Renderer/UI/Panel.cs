using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;

using Microsoft.Xna.Framework;

namespace ClassicUO.Renderer.UI
{
    class Panel : Control
    {
        public Panel(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            WantUpdateSize = false;
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            return base.Draw(batcher, position, hue);
        }
    }

    
}
