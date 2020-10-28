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

            AlphaBlendControl partyGumpBackground = new AlphaBlendControl
            {
                Width = 250,
                Height = 80,
                X = ProfileManager.CurrentProfile.GameWindowSize.X / 2 - 125,
                Y = 150,
                Alpha = 0.2f
            };

            Mobile mobile = World.Mobiles.Get(inviter);

            Label text = new Label
            (
                string.Format
                (
                    ResGumps.P0HasInvitedYouToParty,
                    mobile == null || string.IsNullOrEmpty(mobile.Name) ? ResGumps.NoName : mobile.Name
                ), true, 15
            )
            {
                X = ProfileManager.CurrentProfile.GameWindowSize.X / 2 - 115,
                Y = 165
            };

            NiceButton acceptButton = new NiceButton
                (ProfileManager.CurrentProfile.GameWindowSize.X / 2 + 70, 205, 45, 25, ButtonAction.Activate, ResGumps.Accept);

            NiceButton declineButton = new NiceButton
            (
                ProfileManager.CurrentProfile.GameWindowSize.X / 2 + 10, 205, 45, 25, ButtonAction.Activate, ResGumps.Decline
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
                    NetClient.Socket.Send(new PPartyDecline(World.Party.Inviter));
                    World.Party.Inviter = 0;
                }

                base.Dispose();
            };
        }
    }
}