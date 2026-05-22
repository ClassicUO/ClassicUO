// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Containers
{
    /// <summary>
    /// Tracks the single serial of a container whose contents are awaiting
    /// display in a grid-loot gump.
    /// </summary>
    internal interface IGridLootPending
    {
        uint Serial { get; set; }
    }
}
