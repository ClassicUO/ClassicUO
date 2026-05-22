// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.UI.WorldMap
{
    /// <summary>
    /// Drops entries whose <see cref="WMapEntity.LastUpdate"/> is older
    /// than the TTL window. Throttled so it runs at most once per tick
    /// budget. Reads + mutates the store; owns no state besides its
    /// own next-run timestamp and a scratch removal buffer.
    /// </summary>
    internal interface IWorldMapStalenessFilter
    {
        /// <summary>
        /// Prune stale entries from the store. No-op if the throttle
        /// window has not elapsed since the last call.
        /// </summary>
        void Prune();
    }
}
