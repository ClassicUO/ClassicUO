using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;

namespace ClassicUO.Game.UI.Gumps
{
    class PartyInviteGump
    {
        public void PartyInviteGumpRequest(Serial Inviter)
        {
            var partyGumpBackground = new AlphaBlendControl()
            {
                Width = 250,
                Height = 80,
                X = (Engine.Profile.Current.GameWindowSize.X / 2) - 125,
                Y = 150,
                Alpha = 0.1f
            };

            var text = new Label($"{World.Mobiles.Get(Inviter).Name}\n has invited you to join a party.", true, 15)
            {
                X = (Engine.Profile.Current.GameWindowSize.X / 2) - 115,
                Y = 165,
            };

            var acceptButton = new NiceButton(((Engine.Profile.Current.GameWindowSize.X / 2) + 70), 205, 45, 25, ButtonAction.Activate, "Accept");
            var declineButton = new NiceButton(((Engine.Profile.Current.GameWindowSize.X / 2) + 10), 205, 45, 25, ButtonAction.Activate, "Decline");


            Engine.UI.Add(partyGumpBackground);
            Engine.UI.Add(text);
            Engine.UI.Add(acceptButton);
            Engine.UI.Add(declineButton);

            System.Console.WriteLine(Engine.Ticks);

            acceptButton.MouseUp += (sender, e) =>
            {
                GameActions.RequestPartyAccept(World.Party.Inviter);
                World.Party.Leader = World.Party.Inviter;
                World.Party.Inviter = 0;
                partyGumpBackground.Dispose();
                acceptButton.Dispose();
                declineButton.Dispose();
                text.Dispose();
            };

            declineButton.MouseUp += (sender, e) =>
            {
                NetClient.Socket.Send(new PPartyDecline(World.Party.Inviter));
                World.Party.Leader = 0;
                World.Party.Inviter = 0;
                partyGumpBackground.Dispose();
                acceptButton.Dispose();
                declineButton.Dispose();
                text.Dispose();
            };

            //if (Engine.CurrDateTime )
            //{
            //    partyGumpBackground.Dispose();
            //    acceptButton.Dispose();
            //    declineButton.Dispose();
            //    text.Dispose();
            //}
        }
    }
}
