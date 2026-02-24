// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;

namespace ClassicUO.Game.Map
{
    internal static class TileDetectionHelper
    {
        /// <summary>
        /// Checks if the given tile position has a covering tile above the specified Z level.
        /// A covering tile is a roof or other structure that blocks weather effects and it's not currently rendering
        /// (e.g., hidden roof when player is inside a house or other non-rendering tile above player).
        /// </summary>
        /// <param name="map">The map instance</param>
        /// <param name="targetTileX">Tile X coordinate</param>
        /// <param name="targetTileY">Tile Y coordinate</param>
        /// <param name="playerZ">Player Z coordinate</param>
        /// <returns>True if the position has a non-rendering covering tile above the player, false otherwise.</returns>
        public static bool HasNonRenderingCoveringTile(Map map, int targetTileX, int targetTileY, int playerZ)
        {
            if (map == null) return false;

            Chunk chunk = map.GetChunk(targetTileX, targetTileY, load: false);

            if (chunk == null) return false;

            int pz14 = playerZ + 14; // Threshold for detecting tiles above player

            GameObject obj = chunk.GetHeadObject(targetTileX % 8, targetTileY % 8);

            while (obj != null)
            {
                if (obj.Graphic >= Client.Game.UO.FileManager.TileData.StaticData.Length)
                {
                    obj = obj.TNext;
                    continue;
                }

                ref StaticTiles itemData = ref Client.Game.UO.FileManager.TileData.StaticData[obj.Graphic];

                // Check if tile is above the player and it's not rendering
                if ((sbyte)obj.PriorityZ > pz14 && obj.AlphaHue == 0)
                {
                    return true;
                }

                obj = obj.TNext;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given tile position is a water tile.
        /// </summary>
        /// <param name="map">The map instance</param>
        /// <param name="targetTileX">Tile X coordinate</param>
        /// <param name="targetTileY">Tile Y coordinate</param>
        /// <returns>True if the position is on a water tile, false otherwise.</returns>
        /// <remarks>
        /// Thanks to [markdwags](https://github.com/markdwags) for the code 
        /// in [this comment](https://github.com/ClassicUO/ClassicUO/pull/1852#issuecomment-3656749076).
        /// </remarks>
        public static bool IsWaterTile(Map map, int targetTileX, int targetTileY)
        {
            if (map == null) return false;

            Chunk chunk = map.GetChunk(targetTileX, targetTileY, load: false);

            if (chunk == null) return false;

            // Get the first object in the tile's linked list
            GameObject obj = chunk.Tiles[targetTileX % 8, targetTileY % 8];
            // Find the highest Z-level object (the one that's actually visible)
            GameObject topMostObject = null;
            sbyte highestZ = sbyte.MinValue;

            while (obj != null)
            {
                if ((sbyte)obj.PriorityZ > highestZ &&
                    obj.Graphic < Client.Game.UO.FileManager.TileData.StaticData.Length &&
                    obj.AlphaHue != 0)
                {
                    highestZ = (sbyte)obj.PriorityZ;
                    topMostObject = obj;
                }
                obj = obj.TNext;
            }

            // Now check only the top-most visible object
            if (topMostObject != null)
            {
                switch (topMostObject)
                {
                    case Land land:
                        return land.TileData.IsWet &&
                            (land.TileData.Name?.ToLower().Contains("water") == true);
                    case Static staticTile:
                        return staticTile.ItemData.IsWet &&
                            (staticTile.ItemData.Name?.ToLower().Contains("water") == true);
                }
            }

            return false;
        }
    }
}
