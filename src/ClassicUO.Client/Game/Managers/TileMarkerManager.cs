using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    internal class TileMarkerManager
    {
        public static TileMarkerManager Instance { get; private set; } = new TileMarkerManager();

        private Dictionary<string, ushort> markedTiles = new Dictionary<string, ushort>();

        private TileMarkerManager() { }

        public void AddTile(int x, int y, ushort hue)
        {
            markedTiles.Add($"{x}.{y}", hue);
        }

        public void RemoveTile(int x, int y)
        {
            if (markedTiles.ContainsKey($"{x}.{y}"))
                markedTiles.Remove($"{x}.{y}");
        }

        public bool IsTileMarked(int x, int y, out ushort hue)
        {
            if (markedTiles.TryGetValue($"{x}.{y}", out hue)) return true;
            return false;
        }
    }
}
