// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Facade preserving the legacy static surface
    /// (<c>SeasonManager.LoadSeasonFile()</c>,
    /// <c>SeasonManager.GetSeasonGraphic(...)</c>,
    /// <c>SeasonManager.GetLandSeasonGraphic(...)</c>) while delegating
    /// to three cohesive collaborators: <see cref="ISeasonCalendar"/> reads
    /// <c>seasons.txt</c>, <see cref="ISeasonAssetSwap"/> owns the static-art
    /// remap and <see cref="ISeasonTileRemap"/> owns the land-tile remap.
    /// </summary>
    internal static class SeasonManager
    {
        private static readonly ISeasonAssetSwap _assets = new SeasonAssetSwap();
        private static readonly ISeasonTileRemap _tiles = new SeasonTileRemap();
        private static readonly ISeasonCalendar _calendar = new SeasonCalendar();

        static SeasonManager()
        {
            LoadSeasonFile();
        }

        public static void LoadSeasonFile()
        {
            _calendar.Load(_assets, _tiles);
        }

        public static ushort GetSeasonGraphic(Season season, ushort graphic)
        {
            return _assets.GetSeasonGraphic(season, graphic);
        }

        public static ushort GetLandSeasonGraphic(Season season, ushort graphic)
        {
            return _tiles.GetLandSeasonGraphic(season, graphic);
        }
    }
}
