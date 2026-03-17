// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    internal class PartyInviteGump : Gump
    {
        public PartyInviteGump(World world, uint inviter) : base(world, 0, 0)
        {
            CanCloseWithRightClick = true;

            Mobile mobile = World.Mobiles.Get(inviter);

            var nameWidthAdjustment = mobile == null || mobile.Name.Length < 10 ? 0 : mobile.Name.Length * 5;

            AlphaBlendControl partyGumpBackground = new AlphaBlendControl(World.Context)
            {
                Width = 270 + nameWidthAdjustment,
                Height = 80,
                X = World.Context.Game.Scene.Camera.Bounds.Width / 2 - 125,
                Y = 150,
                Alpha = 0.8f
            };

            Label text = new Label(World.Context, string.Format(ResGumps.P0HasInvitedYouToParty, mobile == null || string.IsNullOrEmpty(mobile.Name) ? ResGumps.NoName : mobile.Name), true, 15)
            {
                X = World.Context.Game.Scene.Camera.Bounds.Width / 2 - 115,
                Y = 165
            };

            NiceButton acceptButton = new NiceButton
            (
                World.Context,
                World.Context.Game.Scene.Camera.Bounds.Width / 2 + 99 + nameWidthAdjustment,
                205,
                45,
                25,
                ButtonAction.Activate,
                ResGumps.Accept
            );

            NiceButton declineButton = new NiceButton
            (
                World.Context,
                World.Context.Game.Scene.Camera.Bounds.Width / 2 + 39 + nameWidthAdjustment,
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
                    GameActions.RequestPartyAccept(World, World.Party.Inviter);
                    World.Party.Leader = World.Party.Inviter;
                    World.Party.Inviter = 0;
                }

                base.Dispose();
            };

            declineButton.MouseUp += (sender, e) =>
            {
                if (World.Party.Inviter != 0 && World.Party.Leader == 0)
                {
                    World.Network.Send_PartyDecline(World.Party.Inviter);
                    World.Party.Inviter = 0;
                }

                base.Dispose();
            };
        }
    }
}