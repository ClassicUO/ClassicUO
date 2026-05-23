// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Boats
{
    /// <summary>
    /// Tracks the entities (mobiles and items) currently riding each boat,
    /// keyed by boat serial, alongside their relative offsets from the boat
    /// anchor. Drives both the per-step entity translation and the
    /// offset reset that happens when steps are cleared.
    /// </summary>
    internal interface IBoatEntityTracker
    {
        /// <summary>
        /// Register or update <paramref name="objSerial"/> as riding boat
        /// <paramref name="serial"/> at the given relative coordinates.
        /// Updates the existing entry if the entity is already tracked.
        /// </summary>
        void PushItemToList(uint serial, uint objSerial, int x, int y, int z);

        /// <summary>Drop the entire riders list for <paramref name="serial"/>.</summary>
        void ClearEntities(uint serial);

        /// <summary>
        /// Zero out the screen-space <c>Offset</c> on every tracked rider
        /// for <paramref name="serial"/> and clear the riders list. Called
        /// when the boat's queued steps are dropped, so riders snap back to
        /// their anchor cell.
        /// </summary>
        void ResetEntityOffsets(World world, uint serial);

        /// <summary>
        /// Move every tracked rider of <paramref name="serial"/> to match
        /// the boat's new logical position (when <paramref name="removeStep"/>
        /// is true) or copy the boat's current screen offset onto each rider
        /// (during interpolation).
        /// </summary>
        void UpdateEntitiesInside
        (
            World world,
            uint serial,
            bool removeStep,
            int x,
            int y,
            int z,
            Direction direction
        );

        /// <summary>Remove all bookkeeping for a boat (called after destroy).</summary>
        void Remove(uint serial);
    }
}
