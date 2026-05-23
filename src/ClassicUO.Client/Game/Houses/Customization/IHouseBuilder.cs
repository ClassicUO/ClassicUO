// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Houses.Customization
{
    /// <summary>
    /// Build-mode interaction for the customization gump: responds to world
    /// targeting (add / erase tile), regenerates the floor preview when the
    /// underlying multi changes, and resets targeting state. Talks to
    /// <see cref="IHouseValidator"/> for rule checks and to
    /// <see cref="Network.NetClient"/> for outbound packets — no file I/O.
    /// </summary>
    internal interface IHouseBuilder
    {
        /// <summary>Re-derive multi-object state flags and the floor preview after any change.</summary>
        void GenerateFloorPlace();

        /// <summary>Handle a world-target click while in customization mode.</summary>
        void OnTargetWorld(GameObject place);

        /// <summary>Reset the world-multi target cursor and clear selection state.</summary>
        void SetTargetMulti();
    }
}
