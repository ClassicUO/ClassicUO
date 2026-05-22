// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Containers
{
    /// <summary>
    /// Holds the serial of the container most recently flagged for grid-loot
    /// display. Consumers set the value when a corpse / container is opened
    /// with the grid-loot flag and reset it once the gump is built.
    /// </summary>
    internal sealed class GridLootPending : IGridLootPending
    {
        public uint Serial { get; set; }
    }
}
