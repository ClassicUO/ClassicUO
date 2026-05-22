// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Targeting
{
    /// <summary>
    /// Bridges incoming <see cref="EventSink.MultiPlacementReceived"/> packets
    /// to the cursor-state's <c>SetTargetingMulti</c> primitive. Keeping it as
    /// its own collaborator isolates the packet-shape mapping from the cursor
    /// state machine and gives <see cref="TargetManager"/> a one-line handler
    /// to forward.
    /// </summary>
    internal interface IMultiPlacementCoordinator
    {
        void OnMultiPlacementReceived(MultiPlacementReceivedArgs e);
    }
}
