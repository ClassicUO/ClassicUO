using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Controls
{
    public class CustomLabelControl : Control
    {
        private readonly RenderedText _customText;

        public CustomLabelControl
        (
            string text,
            bool isunicode,
            ushort hue,
            int maxwidth = 0,
            byte font = 0xFF,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT,
            bool ishtml = false
        )
        {
            _customText = RenderedText.Create
            (
                text,
                hue,
                font,
                isunicode,
                FontStyle.None, // Sem borda ou sombra
                align,
                maxwidth,
                isHTML: ishtml
            );

            AcceptMouseInput = false;
            Width = _customText.Width;
            Height = _customText.Height;
        }

        public string Text
        {
            get => _customText.Text;
            set
            {
                _customText.Text = value;
                Width = _customText.Width;
                Height = _customText.Height;
            }
        }

        public ushort Hue
        {
            get => _customText.Hue;
            set
            {
                if (_customText.Hue != value)
                {
                    _customText.Hue = value;
                    _customText.CreateTexture();
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            _customText.Draw(batcher, x, y, Alpha);

            return base.Draw(batcher, x, y);
        }

        public override void Dispose()
        {
            base.Dispose();
            _customText.Destroy();
        }
    }
}
