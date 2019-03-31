using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    class GumpPicWithWidth : GumpPic
    {
        public GumpPicWithWidth(int x, int y, Graphic graphic, Hue hue, int perc) : base(x, y, graphic, hue)
        {
            Percent = perc;
            AcceptMouseInput = false;
        }

        public int Percent { get; set; }

        public override bool Draw(Batcher2D batcher, Point position)
        {
            return batcher.Draw2DTiled(Texture, new Rectangle(position.X, position.Y, Percent, Height), ShaderHuesTraslator.GetHueVector(Hue));
        }
    }
}
