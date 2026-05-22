// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Facade preserving the legacy static surface
    /// (<c>SeasonManager.LoadSeasonFile()</c>,
    /// <c>SeasonManager.GetSeasonGraphic(...)</c>,
    /// <c>SeasonManager.GetLandSeasonGraphic(...)</c>) while delegating
    /// to cohesive collaborators: <see cref="ISeasonAssetTable"/> owns the
    /// per-season remap arrays and <see cref="ISeasonFileLoader"/> handles
    /// <c>seasons.txt</c> parsing / default-file generation.
    /// </summary>
    internal static class SeasonManager
    {
        private static readonly ISeasonAssetTable _table = new SeasonAssetTable();
        private static readonly ISeasonFileLoader _loader = new SeasonFileLoader();

        static SeasonManager()
        {
            LoadSeasonFile();
        }

        public static void LoadSeasonFile()
        {
            _loader.Load(_table);
        }

        public static ushort GetSeasonGraphic(Season season, ushort graphic)
        {
            return _table.GetSeasonGraphic(season, graphic);
        }

        public static ushort GetLandSeasonGraphic(Season season, ushort graphic)
        {
            return _table.GetLandSeasonGraphic(season, graphic);
        }
    }
}
