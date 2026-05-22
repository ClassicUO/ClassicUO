// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Party
{
    /// <summary>
    /// Party member roster: fixed-size <see cref="Members"/> array, current
    /// <see cref="Leader"/>, and the mutations driven by
    /// <see cref="PartyListUpdatedArgs"/>.
    /// </summary>
    internal interface IPartyRoster
    {
        uint Leader { get; set; }
        PartyMember[] Members { get; }
        bool Contains(uint serial);
        void Clear();
        void HandlePartyListUpdated(PartyListUpdatedArgs e);
    }
}
