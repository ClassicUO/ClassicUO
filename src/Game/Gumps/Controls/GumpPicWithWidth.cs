using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    class GumpPicWithWidth : GumpPic
    {
        public GumpPicWithWidth(int x, int y, Graphic graphic, Hue hue, int perc) : base(x, y, graphic, hue)
        {
            Percent = perc;
        }

        public int Percent { get; set; } = 1;

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            return batcher.Draw2DTiled(Texture, new Rectangle(position.X, position.Y, Percent, Height), ShaderHuesTraslator.GetHueVector(Hue));
        }
    }
}
