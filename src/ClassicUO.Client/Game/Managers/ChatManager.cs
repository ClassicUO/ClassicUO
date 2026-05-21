// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Resources;

namespace ClassicUO.Game.Managers
{
    internal sealed class ChatManager
    {
        private readonly World _world;

        public ChatManager(World world)
        {
            _world = world;
            EventSink.ChatConferenceCreated += OnConferenceCreated;
            EventSink.ChatConferenceDestroyed += OnConferenceDestroyed;
            EventSink.ChatUsernameRequest += OnUsernameRequest;
            EventSink.ChatClosed += OnChatClosed;
            EventSink.ChatUsernameAccepted += OnUsernameAccepted;
            EventSink.ChatUserAdded += OnUserAdded;
            EventSink.ChatUserRemoved += OnUserRemoved;
            EventSink.ChatClearAllPlayers += OnClearAllPlayers;
            EventSink.ChatConferenceJoined += OnConferenceJoined;
            EventSink.ChatConferenceLeft += OnConferenceLeft;
            EventSink.ChatTextReceived += OnTextReceived;
            EventSink.ChatSystemMessage += OnSystemMessage;
        }

        public void Unsubscribe()
        {
            EventSink.ChatConferenceCreated -= OnConferenceCreated;
            EventSink.ChatConferenceDestroyed -= OnConferenceDestroyed;
            EventSink.ChatUsernameRequest -= OnUsernameRequest;
            EventSink.ChatClosed -= OnChatClosed;
            EventSink.ChatUsernameAccepted -= OnUsernameAccepted;
            EventSink.ChatUserAdded -= OnUserAdded;
            EventSink.ChatUserRemoved -= OnUserRemoved;
            EventSink.ChatClearAllPlayers -= OnClearAllPlayers;
            EventSink.ChatConferenceJoined -= OnConferenceJoined;
            EventSink.ChatConferenceLeft -= OnConferenceLeft;
            EventSink.ChatTextReceived -= OnTextReceived;
            EventSink.ChatSystemMessage -= OnSystemMessage;
        }

        private void OnConferenceCreated(ChatConferenceCreatedArgs e)
        {
            CurrentChannelName = e.ChannelName;
            AddChannel(e.ChannelName, e.HasPassword);

            UIManager.GetGump<ChatGump>()?.RequestUpdateContents();
        }

        private void OnConferenceDestroyed(ChatConferenceDestroyedArgs e)
        {
            RemoveChannel(e.ChannelName);

            UIManager.GetGump<ChatGump>()?.RequestUpdateContents();
        }

        private void OnUsernameRequest(ChatUsernameRequestArgs e)
        {
            ChatIsEnabled = ChatStatus.EnabledUserRequest;
        }

        private void OnChatClosed(ChatClosedArgs e)
        {
            Clear();
            ChatIsEnabled = ChatStatus.Disabled;

            UIManager.GetGump<ChatGump>()?.Dispose();
        }

        private void OnUsernameAccepted(ChatUsernameAcceptedArgs e)
        {
            ChatIsEnabled = ChatStatus.Enabled;
            NetClient.Socket.Send_ChatJoinCommand("General");
        }

        private void OnUserAdded(ChatUserAddedArgs e)
        {
            // currently nothing tracked; placeholder for future per-channel user list
        }

        private void OnUserRemoved(ChatUserRemovedArgs e)
        {
            // currently nothing tracked; placeholder for future per-channel user list
        }

        private void OnClearAllPlayers(ChatClearAllPlayersArgs e)
        {
            // currently nothing tracked; placeholder for future per-channel user list
        }

        private void OnConferenceJoined(ChatConferenceJoinedArgs e)
        {
            CurrentChannelName = e.ChannelName;

            UIManager.GetGump<ChatGump>()?.UpdateConference();

            GameActions.Print(
                _world,
                string.Format(ResGeneral.YouHaveJoinedThe0Channel, e.ChannelName),
                ProfileManager.CurrentProfile.ChatMessageHue,
                MessageType.Regular,
                1
            );
        }

        private void OnConferenceLeft(ChatConferenceLeftArgs e)
        {
            GameActions.Print(
                _world,
                string.Format(ResGeneral.YouHaveLeftThe0Channel, e.ChannelName),
                ProfileManager.CurrentProfile.ChatMessageHue,
                MessageType.Regular,
                1
            );
        }

        private void OnTextReceived(ChatTextReceivedArgs e)
        {
            string msgSent = e.Message;

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
                $"{e.Username}: {msgSent}",
                ProfileManager.CurrentProfile.ChatMessageHue,
                MessageType.Regular,
                1
            );
        }

        private void OnSystemMessage(ChatSystemMessageArgs e)
        {
            // TODO: read Chat.enu ?
            // http://docs.polserver.com/packets/index.php?Packet=0xB2

            string msg = GetMessage(e.Cmd - 1);

            if (string.IsNullOrEmpty(msg))
            {
                return;
            }

            string text = e.Text;

            if (!string.IsNullOrEmpty(text))
            {
                int idx = msg.IndexOf("%1");

                if (idx >= 0)
                {
                    msg = msg.Replace("%1", text);
                }

                if (e.Cmd - 1 == 0x000A || e.Cmd - 1 == 0x0017)
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


        public readonly Dictionary<string, ChatChannel> Channels = new Dictionary<string, ChatChannel>();
        public ChatStatus ChatIsEnabled;
        public string CurrentChannelName = string.Empty;

        private static readonly string[] _messages =
        {
            ResGeneral.YouAreAlreadyIgnoringMaximum,
            ResGeneral.YouAreAlreadyIgnoring1,
            ResGeneral.YouAreNowIgnoring1,
            ResGeneral.YouAreNoLongerIgnoring1,
            ResGeneral.YouAreNotIgnoring1,
            ResGeneral.YouAreNoLongerIgnoringAnyone,
            ResGeneral.ThatIsNotAValidConferenceName,
            ResGeneral.ThereIsAlreadyAConference,
            ResGeneral.YouMustHaveOperatorStatus,
            ResGeneral.Conference1RenamedTo2,
            ResGeneral.YouMustBeInAConference,
            ResGeneral.ThereIsNoPlayerNamed1,
            ResGeneral.ThereIsNoConferenceNamed1,
            ResGeneral.ThatIsNotTheCorrectPassword,
            ResGeneral.HasChosenToIgnoreYou,
            ResGeneral.NotGivenYouSpeakingPrivileges,
            ResGeneral.YouCanNowReceivePM,
            ResGeneral.YouWillNoLongerReceivePM,
            ResGeneral.YouAreShowingYourCharName,
            ResGeneral.YouAreNotShowingYourCharName,
            ResGeneral.IsRemainingAnonymous,
            ResGeneral.HasChosenToNotReceivePM,
            ResGeneral.IsKnownInTheLandsOfBritanniaAs2,
            ResGeneral.HasBeenKickedOutOfTheConference,
            ResGeneral.AConferenceModeratorKickedYou,
            ResGeneral.YouAreAlreadyInTheConference1,
            ResGeneral.IsNoLongerAConferenceModerator,
            ResGeneral.IsNowAConferenceModerator,
            ResGeneral.HasRemovedYouFromModerators,
            ResGeneral.HasMadeYouAConferenceModerator,
            ResGeneral.NoLongerHasSpeakingPrivileges,
            ResGeneral.NowHasSpeakingPrivileges,
            ResGeneral.RemovedYourSpeakingPrivileges,
            ResGeneral.GrantedYouSpeakingPrivileges,
            ResGeneral.EveryoneWillHaveSpeakingPrivs,
            ResGeneral.ModeratorsWillHaveSpeakingPrivs,
            ResGeneral.PasswordToTheConferenceChanged,
            ResGeneral.TheConferenceNamed1IsFull,
            ResGeneral.YouAreBanning1FromThisConference,
            ResGeneral.BannedYouFromTheConference,
            ResGeneral.YouHaveBeenBanned
        };


        public static string GetMessage(int index)
        {
            return index < _messages.Length ? _messages[index] : string.Empty;
        }

        public void AddChannel(string text, bool hasPassword)
        {
            if (!Channels.TryGetValue(text, out ChatChannel channel))
            {
                channel = new ChatChannel(text, hasPassword);
                Channels[text] = channel;
            }
        }

        public void RemoveChannel(string name)
        {
            if (Channels.ContainsKey(name))
            {
                Channels.Remove(name);
            }
        }

        public void Clear()
        {
            Channels.Clear();
        }

        //static ChatManager()
        //{
        //    using (StreamReader reader = new StreamReader(File.OpenRead(UOFileManager.GetUOFilePath("Chat.enu"))))
        //    {
        //        while (!reader.EndOfStream)
        //        {
        //            string line = reader.ReadLine();
        //        }
        //    }
        //}
    }
}
