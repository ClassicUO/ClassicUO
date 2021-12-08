#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    internal class PartyInviteGump : Gump
    {
        public PartyInviteGump(uint inviter) : base(0, 0)
        {
            CanCloseWithRightClick = true;

            Mobile mobile = World.Mobiles.Get(inviter);

            var nameWidthAdjustment = mobile == null || mobile.Name.Length < 10 ? 0 : mobile.Name.Length * 5;

            AlphaBlendControl partyGumpBackground = new AlphaBlendControl
            {
                Width = 270 + nameWidthAdjustment,
                Height = 80,
                X = ProfileManager.CurrentProfile.GameWindowSize.X / 2 - 125,
                Y = 150,
                Alpha = 0.8f
            };

            Label text = new Label(string.Format(ResGumps.P0HasInvitedYouToParty, mobile == null || string.IsNullOrEmpty(mobile.Name) ? ResGumps.NoName : mobile.Name), true, 15)
            {
                X = ProfileManager.CurrentProfile.GameWindowSize.X / 2 - 115,
                Y = 165
            };

            NiceButton acceptButton = new NiceButton
            (
                ProfileManager.CurrentProfile.GameWindowSize.X / 2 + 99 + nameWidthAdjustment,
                205,
                45,
                25,
                ButtonAction.Activate,
                ResGumps.Accept
            );

            NiceButton declineButton = new NiceButton
            (
                ProfileManager.CurrentProfile.GameWindowSize.X / 2 + 39 + nameWidthAdjustment,
                205,
                45,
                25,
                ButtonAction.Activate,
                ResGumps.Decline
            );

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
                    NetClient.Socket.Send_PartyDecline(World.Party.Inviter);
                    World.Party.Inviter = 0;
                }

                base.Dispose();
            };
        }
    }
}