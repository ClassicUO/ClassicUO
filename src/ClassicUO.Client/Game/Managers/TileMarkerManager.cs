using ClassicUO.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ClassicUO.Game.Managers
{
    internal class TileMarkerManager
    {
        public static TileMarkerManager Instance { get; private set; } = new TileMarkerManager();

        private Dictionary<string, ushort> markedTiles = new Dictionary<string, ushort>();

        private TileMarkerManager() { Load(); }

        private string savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "TileMarkers.bin");

        public void AddTile(int x, int y, int map, ushort hue)
        {
            markedTiles.Add(FormatLocKey(x, y, map), hue);
        }

        public void RemoveTile(int x, int y, int map)
        {
            if (markedTiles.ContainsKey(FormatLocKey(x, y, map)))
                markedTiles.Remove(FormatLocKey(x, y, map));
        }

        public bool IsTileMarked(int x, int y, int map, out ushort hue)
        {
            if (markedTiles.TryGetValue(FormatLocKey(x, y, map), out hue)) return true;
            return false;
        }

        private string FormatLocKey(int x, int y, int map)
        {
            return $"{x}.{y}.{map}";
        }

        public void Save()
        {
            try
            {
                using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, markedTiles);
                }
            }
            catch { Console.WriteLine("Failed to save marked tile data."); }
        }

        private void Load()
        {
            if(File.Exists(savePath))
                try
                {
                    using (FileStream fs = File.OpenRead(savePath))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        markedTiles = (Dictionary<string, ushort>)bf.Deserialize(fs);
                    }
                }
                catch { }
        }
    }
}
