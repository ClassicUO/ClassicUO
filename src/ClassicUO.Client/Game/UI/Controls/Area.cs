using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    public class Area : Control
    {
        private bool drawBorder;
        private int hue;

        public Area(bool _drawBorder = true, int _borderHue = 0)
        {
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;
            drawBorder = _drawBorder;
            hue = _borderHue;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (drawBorder)
                batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(Color.Gray),
                    x, y,
                    Width-1,
                    Height-1,
                    ShaderHueTranslator.GetHueVector(hue)
                );
            return true;
        }
    }
}
