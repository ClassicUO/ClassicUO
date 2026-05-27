// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.UI.Controls
{
    internal class DataBox : Control
    {
        public DataBox(int x, int y, int w, int h)
        {
            CanMove = false;
            AcceptMouseInput = true;
            X = x;
            Y = y;
            Width = w;
            Height = h;
            WantUpdateSize = false;
        }

        public bool ContainsByBounds { get; set; }

        public void ReArrangeChildren()
        {
            for (int i = 0, height = 0; i < Children.Count; ++i)
            {
                Control c = Children[i];

                if (c.IsVisible && !c.IsDisposed)
                {
                    c.Y = height;

                    height += c.Height;
                }
            }

            WantUpdateSize = true;
        }

        public override bool Contains(int x, int y)
        {
            if (ContainsByBounds)
            {
                return true;
            }

            Control t = null;
            x += ScreenCoordinateX;
            y += ScreenCoordinateY;

            foreach (Control child in Children)
            {
                child.HitTest(x, y, ref t);

                if (t != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}