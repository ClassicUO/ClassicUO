// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Loads the on-disk <c>seasons.txt</c> "calendar" — the per-season list
    /// of static + land-tile remaps — into the supplied asset-swap and
    /// tile-remap tables. Materialises the default file when missing.
    /// One responsibility: file I/O + parsing into the two tables.
    /// </summary>
    internal interface ISeasonCalendar
    {
        /// <summary>Reset both tables then read every line of <c>seasons.txt</c> into them.</summary>
        void Load(ISeasonAssetSwap assets, ISeasonTileRemap tiles);
    }
}
