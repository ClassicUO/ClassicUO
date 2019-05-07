using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal sealed class AlphaBlendControl : Control
    {
        public AlphaBlendControl(float alpha = 0.5f)
        {
            Alpha = alpha;
            AcceptMouseInput = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hue = Vector3.Zero;
            ShaderHuesTraslator.GetHueVector(ref hue, 0, false, Alpha);

            return batcher.Draw2D(CheckerTrans.TransparentTexture, x, y, Width, Height, hue);
        }
    }
}