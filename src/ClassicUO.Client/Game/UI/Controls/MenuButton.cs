namespace ClassicUO.Game.UI.Controls
{
    internal class MenuButton : Control
    {
        public MenuButton(int width, uint hue, float alpha, string tooltip = "")
        {
            Width = width;
            Height = 16;
            AcceptMouseInput = true;
            Area _ = new Area(true, (int)hue) { Width = Width, Height = Height, AcceptMouseInput = false };
            _.Add(new AlphaBlendControl(0.25f) { Width = Width, Height = Height });

            Add(_);
            Add(new Line(0, 2, Width, 2, hue) { Alpha = alpha, AcceptMouseInput = false });
            Add(new Line(0, 7, Width, 2, hue) { Alpha = alpha, AcceptMouseInput = false });
            Add(new Line(0, 12, Width, 2, hue) { Alpha = alpha, AcceptMouseInput = false });
            SetTooltip(tooltip);
            //_.SetTooltip(tooltip);
        }

        public override bool Contains(int x, int y)
        {
            return true;
        }
    }
}
