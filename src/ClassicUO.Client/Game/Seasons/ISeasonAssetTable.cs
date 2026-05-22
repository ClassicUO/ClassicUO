// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Per-season graphic remap table. One responsibility: store the
    /// original -> replacement mapping for static art and land tiles per
    /// <see cref="Season"/>, and look it up at draw time.
    /// </summary>
    internal interface ISeasonAssetTable
    {
        /// <summary>Reset all per-season arrays to empty (zero-filled).</summary>
        void Reset();

        /// <summary>Record a static-art remap (<paramref name="orig"/> -> <paramref name="replace"/>) for <paramref name="season"/>.</summary>
        void SetStatic(Season season, ushort orig, ushort replace);

        /// <summary>Record a land-tile remap (<paramref name="orig"/> -> <paramref name="replace"/>) for <paramref name="season"/>.</summary>
        void SetLand(Season season, ushort orig, ushort replace);

        /// <summary>Return the seasonal static-art replacement for <paramref name="graphic"/>, or <paramref name="graphic"/> when no override is configured.</summary>
        ushort GetSeasonGraphic(Season season, ushort graphic);

        /// <summary>Return the seasonal land-tile replacement for <paramref name="graphic"/>, or <paramref name="graphic"/> when no override is configured.</summary>
        ushort GetLandSeasonGraphic(Season season, ushort graphic);
    }
}
