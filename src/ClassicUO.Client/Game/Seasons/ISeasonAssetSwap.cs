// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Per-season static-art remap. One responsibility: store the
    /// original -> replacement mapping for static graphics per
    /// <see cref="Season"/> and resolve it at draw time.
    /// </summary>
    internal interface ISeasonAssetSwap
    {
        /// <summary>Reset every per-season static-graphic array to empty (zero-filled).</summary>
        void Reset();

        /// <summary>Record a static-art remap (<paramref name="orig"/> -> <paramref name="replace"/>) for <paramref name="season"/>.</summary>
        void Set(Season season, ushort orig, ushort replace);

        /// <summary>Return the seasonal static-art replacement for <paramref name="graphic"/>, or <paramref name="graphic"/> when no override is configured.</summary>
        ushort GetSeasonGraphic(Season season, ushort graphic);
    }
}
