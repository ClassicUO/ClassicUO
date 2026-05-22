// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Resources;

namespace ClassicUO.Game.Chat
{
    /// <summary>
    /// Routes chat text + system messages to the in-game message pipeline.
    /// Owns the (static) lookup table of system message templates and the
    /// formatting rules previously inlined in <c>ChatManager</c>.
    /// </summary>
    internal sealed class ChatMessageRouter : IChatMessageRouter
    {
        private readonly World _world;

        public ChatMessageRouter(World world)
        {
            _world = world;
        }

        public void RouteText(string username, string message)
        {
            string msgSent = message;

            if (!string.IsNullOrEmpty(msgSent))
            {
                int idx = msgSent.IndexOf('{');
                int idxLast = msgSent.IndexOf('}') + 1;

                if (idxLast > idx && idx > -1)
                {
                    msgSent = msgSent.Remove(idx, idxLast - idx);
                }
            }

            GameActions.Print(
                _world,
                $"{username}: {msgSent}",
                ProfileManager.CurrentProfile.ChatMessageHue,
                MessageType.Regular,
                1
            );
        }

        public void RouteSystemMessage(ushort cmd, string text)
        {
            // TODO: read Chat.enu ?
            // http://docs.polserver.com/packets/index.php?Packet=0xB2

            string msg = ChatManager.GetMessage(cmd - 1);

            if (string.IsNullOrEmpty(msg))
            {
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                int idx = msg.IndexOf("%1");

                if (idx >= 0)
                {
                    msg = msg.Replace("%1", text);
                }

                if (cmd - 1 == 0x000A || cmd - 1 == 0x0017)
                {
                    idx = msg.IndexOf("%2");

                    if (idx >= 0)
                    {
                        msg = msg.Replace("%2", text);
                    }
                }
            }

            GameActions.Print(
                _world,
                msg,
                ProfileManager.CurrentProfile.ChatMessageHue,
                MessageType.Regular,
                1
            );
        }

        public void AnnounceConferenceJoined(string channelName)
        {
            GameActions.Print(
                _world,
                string.Format(ResGeneral.YouHaveJoinedThe0Channel, channelName),
                ProfileManager.CurrentProfile.ChatMessageHue,
                MessageType.Regular,
                1
            );
        }

        public void AnnounceConferenceLeft(string channelName)
        {
            GameActions.Print(
                _world,
                string.Format(ResGeneral.YouHaveLeftThe0Channel, channelName),
                ProfileManager.CurrentProfile.ChatMessageHue,
                MessageType.Regular,
                1
            );
        }
    }
}
