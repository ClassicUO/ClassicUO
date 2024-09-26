using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    public class ResizableStaticPic : Control
    {
        private uint graphic;
        private ushort hue = 0;
        private Vector3 hueVector { get; set; } = ShaderHueTranslator.GetHueVector(0, false, 1);

        public ushort Hue
        {
            get { return hue; }
            set
            {
                hue = value;
                hueVector = ShaderHueTranslator.GetHueVector(hue, false, 1);
            }
        }
        public uint Graphic { get { return graphic; } set { graphic = value; } }

        public ResizableStaticPic(uint graphic, int width, int height)
        {
            this.graphic = graphic;

            Width = width;
            Height = height;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (IsDisposed)
            {
                return false;
            }

            ref readonly var texture = ref Client.Game.Arts.GetArt(graphic);

            Rectangle _rect = Client.Game.Arts.GetRealArtBounds(graphic);

            Point _originalSize = new Point(Width, Height);
            Point _point = new Point((Width >> 1) - (_originalSize.X >> 1), (Height >> 1) - (_originalSize.Y >> 1));

            if (_rect.Width < Width)
            {
                _originalSize.X = _rect.Width;
                _point.X = (Width >> 1) - (_originalSize.X >> 1);
            }

            if (_rect.Height < Height)
            {
                _originalSize.Y = _rect.Height;
                _point.Y = (Height >> 1) - (_originalSize.Y >> 1);
            }

            if (_rect.Width > Width)
            {
                _originalSize.X = Width;
                _point.X = 0;
            }

            if (_rect.Height > Height)
            {
                _originalSize.Y = Height;
                _point.Y = 0;
            }

            if (texture.Texture != null)
            {
                batcher.Draw
                (
                    texture.Texture,
                    new Rectangle
                    (
                        x + _point.X,
                        y + _point.Y,
                        _originalSize.X,
                        _originalSize.Y
                    ),
                    new Rectangle
                    (
                        texture.UV.X + _rect.X,
                        texture.UV.Y + _rect.Y,
                        _rect.Width,
                        _rect.Height
                    ),
                    hueVector
                );

                return true;
            }

            return false;
        }
    }
}
