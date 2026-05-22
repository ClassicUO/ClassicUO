// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Targeting
{
    /// <summary>
    /// Concrete <see cref="IMultiPlacementCoordinator"/>. Translates the raw
    /// multi-placement packet args into a <c>SetTargetingMulti</c> call on the
    /// cursor state.
    /// </summary>
    internal sealed class MultiPlacementCoordinator : IMultiPlacementCoordinator
    {
        private readonly ITargetCursorState _cursorState;

        public MultiPlacementCoordinator(ITargetCursorState cursorState)
        {
            _cursorState = cursorState;
        }

        public void OnMultiPlacementReceived(MultiPlacementReceivedArgs e)
        {
            _cursorState.SetTargetingMulti(e.TargetId, e.MultiId, e.OffsetX, e.OffsetY, e.OffsetZ, e.Hue);
        }
    }
}
