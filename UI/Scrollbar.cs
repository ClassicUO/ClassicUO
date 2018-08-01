using ClassicUO.Input;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.UI
{
    public sealed class Scrollbar : Control
    {
        private const int BASE_WIDTH = 5;

        private readonly ScrollbarRail _rail;
        private int _delta;

        public Scrollbar(in Panel parent, in int x, in  int y) : base(parent, x, y, BASE_WIDTH, parent.Height)
        {
            _rail = new ScrollbarRail(parent, this);
        }


        public Texture2D Texture { get; set; }

        public override void OnMouseMove(in MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        public override void OnMouseWheel(in MouseWheelEventArgs e)
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

            public ScrollbarRail(in Panel parent, in Scrollbar scrollbar) : base(parent, scrollbar.X, scrollbar.Y, scrollbar.Width, scrollbar.Height)
            {
                _scrollbar = scrollbar;
            }

            public override void OnMouseWheel(in MouseWheelEventArgs e)
            {
                _scrollbar.OnMouseWheel(e);
            }
        }
    }
}