using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class SimpleBorder : Control
    {
        public ushort Hue
        {
            get => hue;
            set
            {
                hue = value;
                hueVector = ShaderHueTranslator.GetHueVector(value, false, Alpha);
            }
        }

        private int _width = 0, _height = 0;
        private Vector3 hueVector;
        private ushort hue = 0;

        //Return 0 so this control has a 0, 0 size to not interfere with hitboxes
        public new int Width { get { return 0; } set { _width = value; } }
        public new int Height { get { return 0; } set { _height = value; } }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            base.Draw(batcher, x, y);

            if (hueVector == default)
            {
                hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha);
            }

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.White),
                x, y,
                _width, _height,
                hueVector
                );

            return true;
        }
    }
}
