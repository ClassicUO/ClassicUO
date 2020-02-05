#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    class PartyInviteGump : Gump
    {
        public PartyInviteGump(uint inviter) : base(0, 0)
        {
            CanCloseWithRightClick = true;
            var partyGumpBackground = new AlphaBlendControl()
            {
                Width = 250,
                Height = 80,
                X = (ProfileManager.Current.GameWindowSize.X / 2) - 125,
                Y = 150,
                Alpha = 0.2f
            };

            Mobile mobile = World.Mobiles.Get(inviter);

            var text = new Label($"{ (mobile == null || string.IsNullOrEmpty(mobile.Name) ? "[no-name]" : mobile.Name) }\n has invited you to join a party.", true, 15)
            {
                X = (ProfileManager.Current.GameWindowSize.X / 2) - 115,
                Y = 165,
            };

            var acceptButton = new NiceButton(((ProfileManager.Current.GameWindowSize.X / 2) + 70), 205, 45, 25, ButtonAction.Activate, "Accept");
            var declineButton = new NiceButton(((ProfileManager.Current.GameWindowSize.X / 2) + 10), 205, 45, 25, ButtonAction.Activate, "Decline");

            Add(partyGumpBackground);
            Add(text);
            Add(acceptButton);
            Add(declineButton);

            acceptButton.MouseUp += (sender, e) =>
            {
                if (World.Party.Inviter != 0 && World.Party.Leader == 0)
                {
                    GameActions.RequestPartyAccept(World.Party.Inviter);
                    World.Party.Leader = World.Party.Inviter;
                    World.Party.Inviter = 0;
                }
                base.Dispose();
            };

            declineButton.MouseUp += (sender, e) =>
            {
                if (World.Party.Inviter != 0 && World.Party.Leader == 0)
                {
                    NetClient.Socket.Send(new PPartyDecline(World.Party.Inviter));
                    World.Party.Inviter = 0;
                }
                base.Dispose();
            };
        }
    }
}
