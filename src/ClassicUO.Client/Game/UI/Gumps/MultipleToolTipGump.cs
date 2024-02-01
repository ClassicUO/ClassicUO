using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class MultipleToolTipGump : Gump
    {
        private readonly CustomToolTip[] toolTips;
        private readonly Control hoverReference;

        public static bool SSIsEnabled = false;

        public static int SSX, SSY;
        public static int SSWidth, SSHeight;

        public MultipleToolTipGump(int x, int y, CustomToolTip[] toolTips, Controls.Control hoverReference) : base(0, 0)
        {
            this.toolTips = toolTips;
            this.hoverReference = hoverReference;
            BuildGump();
            WantUpdateSize = true;

            X = x;
            Y = y;

            SSIsEnabled = true;
        }

        private void BuildGump()
        {
            for (int i = 0; i < toolTips.Length; i++)
            {
                if (toolTips[i] == null)
                    continue;
                toolTips[i].OnOPLLoaded += () => { RepositionTooltips(); };
                Add(toolTips[i]);
            }
            RepositionTooltips();
        }

        private void RepositionTooltips()
        {
            int x = 0, totalWidth = 0, totalHeight = 0;
            for (int i = 0; i < toolTips.Length; i++)
            {
                if (toolTips[i] == null)
                    continue;
                toolTips[i].X = x;
                toolTips[i].Y = 0;
                toolTips[i].RemoveHoverReference();
                totalWidth += toolTips[i].Width;

                x += toolTips[i].Width + 14;

                if (totalHeight < toolTips[i].Height)
                    totalHeight = toolTips[i].Height;
            }
            ForceSizeUpdate();
            SSWidth = Width + 9;
            SSHeight = Height + 9;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!hoverReference.MouseIsOver)
                Dispose();

            if (Height == 0)
            {
                foreach (Control c in Children)
                    if (Height < c.Height)
                        Height = c.Height;
            }

            int z_width = Width + 24;
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

            SSX = x - 4;
            SSY = y - 2;

            return base.Draw(batcher, x, y);

        }

        public override void Dispose()
        {
            base.Dispose();
            SSIsEnabled = false;
        }
    }
}
