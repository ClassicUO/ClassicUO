using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class TextureControl : Control
    {
        public TextureControl()
        {
            CanMove = true;
            AcceptMouseInput = true;
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            return batcher.Draw2D(Texture, new Rectangle(position.X, position.Y, Width, Height), new Rectangle(0, 0, Texture.Width, Texture.Height), Vector3.Zero);
        }
    }
}
