using ClassicUO.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    internal class TileMarkerManager
    {
        public static TileMarkerManager Instance { get; private set; } = new TileMarkerManager();

        private Dictionary<string, ushort> markedTiles = new Dictionary<string, ushort>();

        private TileMarkerManager() { LoadSync(); }

        private string savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "TileMarkers.bin");

        public void AddTile(int x, int y, int map, ushort hue)
        {
            if (markedTiles.TryGetValue(FormatLocKey(x, y, map), out hue)) {
                markedTiles.Add(FormatLocKey(x, y, map), hue);
            } else
            {
                markedTiles.Add(FormatLocKey(x, y, map), hue);
            }
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

        public async void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(markedTiles);
                File.WriteAllText(savePath, json);
            }
            catch { Console.WriteLine("Failed to save marked tile data."); }
        }

        private void LoadSync()
        {
            if(File.Exists(savePath))
                try
                {
                    string json = File.ReadAllText(savePath);
                    markedTiles = JsonSerializer.Deserialize<Dictionary<string, ushort>>(json) ?? new Dictionary<string, ushort>();
                }
                catch { }
        }

        private async void Load()
        {
            if(File.Exists(savePath))
                try
                {
                    string json = File.ReadAllText(savePath);
                    markedTiles = JsonSerializer.Deserialize<Dictionary<string, ushort>>(json) ?? new Dictionary<string, ushort>();
                }
                catch { }
        }
    }
}
