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

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            return batcher.Draw2DTiled(Texture, x, y, Percent, Height, ShaderHuesTraslator.GetHueVector(Hue));
        }
    }
}
