using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class SimpleBorder : Control
    {
        public ushort Hue = 0;

        private int _width = 0, _height = 0;

        //Return 0 so this control has a 0, 0 size to not interfere with hitboxes
        public int Width { get { return 0; } set { _width = value; } }
        public int Height { get { return 0; } set { _height = value; } }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;
            base.Draw(batcher, x, y);

            var huevec = ShaderHueTranslator.GetHueVector(Hue, false, Alpha);

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.White),
                x, y,
                _width, _height,
                huevec
                );

            return true;
        }
    }
}
