// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Per-season land-tile remap. One responsibility: store the
    /// original -> replacement mapping for land tiles per
    /// <see cref="Season"/> and resolve it at draw time.
    /// </summary>
    internal interface ISeasonTileRemap
    {
        /// <summary>Reset every per-season land-tile array to empty (zero-filled).</summary>
        void Reset();

        /// <summary>Record a land-tile remap (<paramref name="orig"/> -> <paramref name="replace"/>) for <paramref name="season"/>.</summary>
        void Set(Season season, ushort orig, ushort replace);

        /// <summary>Return the seasonal land-tile replacement for <paramref name="graphic"/>, or <paramref name="graphic"/> when no override is configured.</summary>
        ushort GetLandSeasonGraphic(Season season, ushort graphic);
    }
}
