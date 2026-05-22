// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Party
{
    /// <summary>
    /// Routes party chat events: when the speaker matches a roster member, the
    /// message is forwarded through <see cref="MessageManager.HandleMessage"/>
    /// so it lands in the journal and any active overhead/text surfaces.
    /// </summary>
    internal sealed class PartyChatRouter : IPartyChatRouter
    {
        private readonly World _world;
        private readonly IPartyRoster _roster;

        public PartyChatRouter(World world, IPartyRoster roster)
        {
            _world = world;
            _roster = roster;
        }

        public void HandlePartyChatMessage(PartyChatMessageArgs e)
        {
            PartyMember[] members = _roster.Members;

            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] != null && members[i].Serial == e.Serial)
                {
                    _world.MessageManager.HandleMessage
                    (
                        null,
                        e.Text,
                        members[i].Name,
                        ProfileManager.CurrentProfile.PartyMessageHue,
                        MessageType.Party,
                        3,
                        TextType.GUILD_ALLY
                    );

                    break;
                }
            }
        }
    }
}
