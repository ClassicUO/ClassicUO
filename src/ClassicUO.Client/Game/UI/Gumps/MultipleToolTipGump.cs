using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MultipleToolTipGump : Gump
    {
        private readonly CustomToolTip[] toolTips;
        private readonly Control hoverReference;

        public MultipleToolTipGump(int x, int y, CustomToolTip[] toolTips, Controls.Control hoverReference) : base(0, 0)
        {
            this.toolTips = toolTips;
            this.hoverReference = hoverReference;
            BuildGump();
            WantUpdateSize = true;

            X = x;
            Y = y;
        }

        private void BuildGump()
        {
            int x = 0, totalWidth = 0, totalHeight = 0;
            for (int i = 0; i < toolTips.Length; i++)
            {
                if (toolTips[i] == null)
                    continue;
                toolTips[i].X = x;
                toolTips[i].Y = 0;
                toolTips[i].RemoveHoverReference();
                Add(toolTips[i]);
                totalWidth += toolTips[i].Width;

                x += toolTips[i].Width + 16;

                if (totalHeight < toolTips[i].Height)
                    totalHeight = toolTips[i].Height;
            }
            Width = totalWidth;
            Height = totalHeight;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!hoverReference.MouseIsOver)
                Dispose();

            if(Height == 0)
            {
                foreach(Control c in Children)
                    if (Height < c.Height)
                        Height = c.Height;
            }

            int z_width = Width + 16;
            int z_height = Height + 8;

            if (x < 0)
            {
                x = 0;
            }
            else if (x > Client.Game.Window.ClientBounds.Width - z_width)
            {
                x = Client.Game.Window.ClientBounds.Width - z_width;
            }

            if (y < 0)
            {
                y = 0;
            }
            else if (y > Client.Game.Window.ClientBounds.Height - z_height)
            {
                y = Client.Game.Window.ClientBounds.Height - z_height;
            }

            return base.Draw(batcher, x, y);

        }
    }
}
