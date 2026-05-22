// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Parses the on-disk <c>seasons.txt</c> file and populates an
    /// <see cref="ISeasonAssetTable"/>. Materialises the default file when
    /// missing. One responsibility: file I/O + parsing.
    /// </summary>
    internal interface ISeasonFileLoader
    {
        /// <summary>Reset <paramref name="table"/> then read every line of <c>seasons.txt</c> into it.</summary>
        void Load(ISeasonAssetTable table);
    }
}
