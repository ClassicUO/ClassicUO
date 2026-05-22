// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Party
{
    /// <summary>
    /// Owns the <see cref="Members"/> array and <see cref="Leader"/> serial.
    /// All roster mutations are driven by <see cref="PartyListUpdatedArgs"/>
    /// via <see cref="HandlePartyListUpdated"/>. When the update collapses the
    /// roster (count &lt;= 1) the paired <see cref="IPartyInviteCoordinator"/>
    /// is asked to clear its pending invite so the facade exposes a single
    /// consistent "empty party" state.
    /// </summary>
    internal sealed class PartyRoster : IPartyRoster
    {
        public const int PARTY_SIZE = 10;

        private readonly World _world;
        private readonly IPartyInviteCoordinator _inviteCoordinator;

        public PartyRoster(World world, IPartyInviteCoordinator inviteCoordinator)
        {
            _world = world;
            _inviteCoordinator = inviteCoordinator;
        }

        public uint Leader { get; set; }

        public PartyMember[] Members { get; } = new PartyMember[PARTY_SIZE];

        public bool Contains(uint serial)
        {
            for (int i = 0; i < PARTY_SIZE; i++)
            {
                PartyMember mem = Members[i];

                if (mem != null && mem.Serial == serial)
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            Leader = 0;
            _inviteCoordinator.ClearInviter();

            for (int i = 0; i < PARTY_SIZE; i++)
            {
                Members[i] = null;
            }
        }

        public void HandlePartyListUpdated(PartyListUpdatedArgs e)
        {
            bool add = e.IsAdd;
            IReadOnlyList<uint> serials = e.Serials;
            int count = serials?.Count ?? 0;

            if (count <= 1)
            {
                Leader = 0;
                _inviteCoordinator.ClearInviter();

                for (int i = 0; i < PARTY_SIZE; i++)
                {
                    if (Members[i] == null || Members[i].Serial == 0)
                    {
                        break;
                    }

                    BaseHealthBarGump gump = UIManager.GetGump<BaseHealthBarGump>(Members[i].Serial);

                    if (gump != null)
                    {
                        if (!add)
                        {
                            Members[i].Serial = 0;
                        }

                        gump.RequestUpdateContents();
                    }
                }

                Clear();

                UIManager.GetGump<PartyGump>()?.RequestUpdateContents();

                return;
            }

            Clear();

            uint to_remove = 0xFFFF_FFFF;

            if (!add)
            {
                to_remove = e.RemovedSerial;
                UIManager.GetGump<BaseHealthBarGump>(to_remove)?.RequestUpdateContents();
            }

            bool remove_all = !add && to_remove == _world.Player;
            int done = 0;

            for (int i = 0; i < count; i++)
            {
                uint serial = serials[i];
                bool remove = !add && serial == to_remove;

                if (remove && i == 0)
                {
                    remove_all = true;
                }

                if (!remove && !remove_all)
                {
                    if (!Contains(serial))
                    {
                        Members[i] = new PartyMember(_world, serial);
                    }

                    done++;
                }

                if (i == 0 && !remove && !remove_all)
                {
                    Leader = serial;
                }

                UIManager.GetGump<BaseHealthBarGump>(serial)?.RequestUpdateContents();
            }

            if (done <= 1 && !add)
            {
                for (int i = 0; i < PARTY_SIZE; i++)
                {
                    if (Members[i] != null && SerialHelper.IsValid(Members[i].Serial))
                    {
                        uint serial = Members[i].Serial;
                        Members[i] = null;
                        UIManager.GetGump<BaseHealthBarGump>(serial)?.RequestUpdateContents();
                    }
                }

                Clear();
            }

            UIManager.GetGump<PartyGump>()?.RequestUpdateContents();
        }
    }
}
