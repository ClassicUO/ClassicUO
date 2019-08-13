using System;

using ClassicUO.Input;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GumpMinimizer : Gump
    {
        private Control _mainGump;
        private readonly Button _minimized;
        internal GumpMinimizer(Control maingump, ushort minGraph, ushort maxGraphUnused, ushort maxGraphUsed, int x, int y, string minText, ushort normalHue, string maxText = "", byte font = 0, ushort maxGraphOver = 0, ushort maxOverHue = 65535, bool isunicode = true) : base(0,0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            _mainGump = maingump;
            Button b = new Button(int.MaxValue, maxGraphUnused, maxGraphUsed, maxGraphOver, maxText, font, isunicode, normalHue, maxOverHue) { X = x, Y = y };
            b.MouseUp += MainMouseClick;
            _mainGump.Add(b);
            _mainGump.Disposed += MainDisposed;
            Add(_minimized = new Button(int.MaxValue - 1, minGraph, minGraph, 0, minText, font, isunicode, normalHue));
            WantUpdateSize = false;
            Height = _minimized.Height;
            Width = _minimized.Width;
            IsVisible = false;
        }

        private void MainDisposed(object sender, EventArgs e)
        {
            _mainGump = null;
            Dispose();
        }

        public override void Dispose()
        {
            if(_mainGump != null && !_mainGump.IsDisposed)
            {
                Control copy = _mainGump;
                _mainGump = null;
                copy.Dispose();
            }
            base.Dispose();
        }

        private void MainMouseClick(object sender, Input.MouseEventArgs e)
        {
            if(e.Button == Input.MouseButton.Left && _mainGump.IsVisible)
            {
                _mainGump.IsVisible = false;
                X = _mainGump.ScreenCoordinateX;
                Y = _mainGump.ScreenCoordinateY;
                IsVisible = true;
                BringOnTop();
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left && IsVisible)
            {
                IsVisible = false;
                _mainGump.X = _minimized.ScreenCoordinateX;
                _mainGump.Y = _minimized.ScreenCoordinateY;
                _mainGump.IsVisible = true;
                _mainGump.BringOnTop();
            }
            return base.OnMouseDoubleClick(x, y, button);
        }
    }
}
