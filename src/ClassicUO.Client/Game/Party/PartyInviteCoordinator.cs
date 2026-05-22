// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Party
{
    /// <summary>
    /// Tracks the pending <see cref="Inviter"/> serial. When the user has the
    /// invite prompt enabled in their profile this also surfaces the
    /// <see cref="PartyInviteGump"/>. Clearing the inviter is exposed so the
    /// roster can keep facade state coherent on empty-party transitions.
    /// </summary>
    internal sealed class PartyInviteCoordinator : IPartyInviteCoordinator
    {
        private readonly World _world;

        public PartyInviteCoordinator(World world)
        {
            _world = world;
        }

        public uint Inviter { get; set; }

        public void ClearInviter() => Inviter = 0;

        public void HandlePartyInviteReceived(PartyInviteReceivedArgs e)
        {
            Inviter = e.Inviter;

            if (ProfileManager.CurrentProfile.PartyInviteGump)
            {
                UIManager.Add(new PartyInviteGump(_world, Inviter));
            }
        }
    }
}
