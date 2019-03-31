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
        private readonly Color _color;
        public Panel(int x, int y, int w, int h, Color color)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;
            _color = color;
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            batcher.Draw2D(Textures.GetTexture(_color), x, y, Width, Height, Vector3.Zero);
            return base.Draw(batcher, x, y);
        }
    }

    
}
