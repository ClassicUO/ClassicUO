using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.UI.Controls
{
    internal class ClickableColorBox : Control
    {
        private const int CELL = 12;

        private readonly ColorBox _colorBox;

        public ClickableColorBox
        (
            int x,
            int y,
            int w,
            int h,
            ushort hue,
            uint color
        )
        {
            X = x;
            Y = y;
            WantUpdateSize = false;

            GumpPic background = new GumpPic(0, 0, 0x00D4, 0);
            Add(background);
            _colorBox = new ColorBox(w, h, hue, color);
            _colorBox.X = 3;
            _colorBox.Y = 3;
            Add(_colorBox);

            Width = background.Width;
            Height = background.Height;
        }

        public ushort Hue => _colorBox.Hue;


        public void SetColor(ushort hue, uint pol)
        {
            _colorBox.SetColor(hue, pol);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                UIManager.GetGump<ColorPickerGump>()?.Dispose();

                ColorPickerGump pickerGump = new ColorPickerGump
                    (0, 0, 100, 100, s => _colorBox.SetColor(s, HuesLoader.Instance.GetPolygoneColor(CELL, s)));

                UIManager.Add(pickerGump);
            }
        }
    }
}