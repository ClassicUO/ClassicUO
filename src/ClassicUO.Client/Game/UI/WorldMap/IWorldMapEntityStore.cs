// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.UI.WorldMap
{
    /// <summary>
    /// Per-serial store of <see cref="WMapEntity"/> tracked positions
    /// (party / guild members). Single responsibility: add / update /
    /// remove / lookup. No range filtering, no network, no event subs.
    /// </summary>
    internal interface IWorldMapEntityStore
    {
        /// <summary>
        /// Underlying serial → entity dictionary. Exposed so callers
        /// can enumerate <c>.Values</c> directly (legacy surface).
        /// </summary>
        Dictionary<uint, WMapEntity> Entities { get; }

        /// <summary>
        /// Insert a new entity for <paramref name="serial"/> or update
        /// the existing one in-place. Returns the resulting entity.
        /// </summary>
        WMapEntity AddOrUpdate(uint serial, int x, int y, int hp, int map, bool isguild, string name);

        bool TryGet(uint serial, out WMapEntity entity);

        void Remove(uint serial);

        void Clear();
    }
}
