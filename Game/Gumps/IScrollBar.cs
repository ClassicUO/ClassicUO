using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    internal interface IScrollBar
    {
        int Value { get; set; }
        int MinValue { get; set; }
        int MaxValue { get; set; }
        Point Location { get; set; }
        int Width { get; set; }
        int Height { get; set; }

        bool Contains(int x, int y);

        bool IsVisible { get; set; } // from AControl
    }
}