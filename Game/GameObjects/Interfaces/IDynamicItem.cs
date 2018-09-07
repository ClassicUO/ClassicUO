
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.GameObjects.Interfaces
{
    public interface IDynamicItem
    {
        StaticTiles ItemData { get; }

        Graphic Graphic { get; set; }
        Position Position { get; set; }

        bool IsAtWorld(int x,  int y);
    }
}