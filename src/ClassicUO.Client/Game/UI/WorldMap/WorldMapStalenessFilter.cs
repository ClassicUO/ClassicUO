// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.UI.WorldMap
{
    /// <summary>
    /// TTL-based pruner for the world-map entity store. Entries older
    /// than ~1s since their last update are dropped. The pruner itself
    /// is throttled to run at most once per second to bound work.
    /// </summary>
    internal sealed class WorldMapStalenessFilter : IWorldMapStalenessFilter
    {
        private const long TickWindowMs = 1000;

        private readonly IWorldMapEntityStore _store;
        private readonly List<WMapEntity> _toRemove = new List<WMapEntity>();
        private uint _lastUpdate;

        public WorldMapStalenessFilter(IWorldMapEntityStore store)
        {
            _store = store;
        }

        public void Prune()
        {
            if (_lastUpdate > Time.Ticks)
            {
                return;
            }

            _lastUpdate = Time.Ticks + (uint)TickWindowMs;

            long ticks = Time.Ticks - TickWindowMs;

            foreach (WMapEntity entity in _store.Entities.Values)
            {
                if (entity.LastUpdate < ticks)
                {
                    _toRemove.Add(entity);
                }
            }

            if (_toRemove.Count != 0)
            {
                foreach (WMapEntity entity in _toRemove)
                {
                    _store.Remove(entity.Serial);
                }

                _toRemove.Clear();
            }
        }
    }
}
