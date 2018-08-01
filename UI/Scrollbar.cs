using ClassicUO.Input;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.UI
{
    public sealed class Scrollbar : Control
    {
        private const int BASE_WIDTH = 5;

        private readonly ScrollbarRail _rail;
        private int _delta;

        public Scrollbar(Panel parent, int x, int y) : base(parent, x, y, BASE_WIDTH, parent.Height)
        {
            _rail = new ScrollbarRail(parent, this);
        }


        public Texture2D Texture { get; set; }

        public override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        public override void OnMouseWheel(MouseWheelEventArgs e)
        {
            switch (e.Direction)
            {
                case WheelDirection.Down:
                    break;
                case WheelDirection.Up:
                    break;
            }

            base.OnMouseWheel(e);
        }


        private sealed class ScrollbarRail : Control
        {
            private readonly Scrollbar _scrollbar;

            public ScrollbarRail(Panel parent, Scrollbar scrollbar) : base(parent, scrollbar.X, scrollbar.Y, scrollbar.Width, scrollbar.Height)
            {
                _scrollbar = scrollbar;
            }

            public override void OnMouseWheel(MouseWheelEventArgs e)
            {
                _scrollbar.OnMouseWheel(e);
            }
        }
    }
}