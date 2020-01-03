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
