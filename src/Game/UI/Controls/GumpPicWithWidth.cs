using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class GumpPicWithWidth : GumpPic
    {
        public GumpPicWithWidth(int x, int y, Graphic graphic, Hue hue, int perc) : base(x, y, graphic, hue)
        {
            Percent = perc;
            AcceptMouseInput = false;
        }

        public int Percent { get; set; }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hue = Vector3.Zero;
            ShaderHuesTraslator.GetHueVector(ref hue, Hue);

            return batcher.Draw2DTiled(Texture, x, y, Percent, Height, hue);
        }
    }
}