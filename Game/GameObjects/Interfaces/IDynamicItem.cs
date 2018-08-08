using ClassicUO.AssetsLoader;

namespace ClassicUO.Game.WorldObjects.Interfaces
{
    public interface IDynamicItem
    {
        StaticTiles ItemData { get; }

        Graphic Graphic { get; set; }
        Position Position { get; set; }

        bool IsAtWorld(in int x, in int y);
    }
}