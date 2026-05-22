// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Party
{
    /// <summary>
    /// Tracks the pending party <see cref="Inviter"/> serial and (optionally)
    /// surfaces the invite prompt gump when a
    /// <see cref="PartyInviteReceivedArgs"/> arrives.
    /// </summary>
    internal interface IPartyInviteCoordinator
    {
        uint Inviter { get; set; }
        void ClearInviter();
        void HandlePartyInviteReceived(PartyInviteReceivedArgs e);
    }
}
