// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Chat;
using ClassicUO.Game.Events;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
using ClassicUO.Resources;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Facade over the three Chat collaborators: keeps the existing public
    /// surface (<c>_world.ChatManager.X</c>) and owns the EventSink
    /// subscriptions. Channel storage, user roster and message routing live
    /// in dedicated cohesive classes under <see cref="Game.Chat"/>.
    /// </summary>
    internal sealed class ChatManager : IEventListener
    {
        private readonly IChatChannelStore _channels;
        private readonly IChatRoster _roster;
        private readonly IChatMessageRouter _router;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public ChatManager(World world)
            : this(new ChatChannelStore(), new ChatRoster(), new ChatMessageRouter(world))
        {
        }

        /// <summary>Full DI seam — inject all three collaborators.</summary>
        internal ChatManager(IChatChannelStore channels, IChatRoster roster, IChatMessageRouter router)
        {
            _channels = channels;
            _roster = roster;
            _router = router;
        }

        public void Subscribe()
        {
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

        // ---- Public facade: channel store ----
        public Dictionary<string, ChatChannel> Channels => _channels.Channels;

        public string CurrentChannelName
        {
            get => _channels.CurrentChannelName;
            set => _channels.CurrentChannelName = value;
        }

        public ChatStatus ChatIsEnabled
        {
            get => _channels.ChatIsEnabled;
            set => _channels.ChatIsEnabled = value;
        }

        public void AddChannel(string text, bool hasPassword) => _channels.AddChannel(text, hasPassword);
        public void RemoveChannel(string name) => _channels.RemoveChannel(name);
        public void Clear() => _channels.Clear();

        // ---- Event handlers ----
        private void OnConferenceCreated(ChatConferenceCreatedArgs e)
        {
            _channels.CurrentChannelName = e.ChannelName;
            _channels.AddChannel(e.ChannelName, e.HasPassword);

            UIManager.GetGump<ChatGump>()?.RequestUpdateContents();
        }

        private void OnConferenceDestroyed(ChatConferenceDestroyedArgs e)
        {
            _channels.RemoveChannel(e.ChannelName);

            UIManager.GetGump<ChatGump>()?.RequestUpdateContents();
        }

        private void OnUsernameRequest(ChatUsernameRequestArgs e)
        {
            _channels.ChatIsEnabled = ChatStatus.EnabledUserRequest;
        }

        private void OnChatClosed(ChatClosedArgs e)
        {
            _channels.Clear();
            _channels.ChatIsEnabled = ChatStatus.Disabled;

            UIManager.GetGump<ChatGump>()?.Dispose();
        }

        private void OnUsernameAccepted(ChatUsernameAcceptedArgs e)
        {
            _channels.ChatIsEnabled = ChatStatus.Enabled;
            _roster.Username = e.Username;
            _roster.IsUsernameAccepted = true;
            NetClient.Socket.Send_ChatJoinCommand("General");
        }

        private void OnUserAdded(ChatUserAddedArgs e)
        {
            _roster.AddUser(e.UserType, e.Username);
        }

        private void OnUserRemoved(ChatUserRemovedArgs e)
        {
            _roster.RemoveUser(e.Username);
        }

        private void OnClearAllPlayers(ChatClearAllPlayersArgs e)
        {
            _roster.ClearAllPlayers();
        }

        private void OnConferenceJoined(ChatConferenceJoinedArgs e)
        {
            _channels.CurrentChannelName = e.ChannelName;

            UIManager.GetGump<ChatGump>()?.UpdateConference();

            _router.AnnounceConferenceJoined(e.ChannelName);
        }

        private void OnConferenceLeft(ChatConferenceLeftArgs e)
        {
            _router.AnnounceConferenceLeft(e.ChannelName);
        }

        private void OnTextReceived(ChatTextReceivedArgs e)
        {
            _router.RouteText(e.Username, e.Message);
        }

        private void OnSystemMessage(ChatSystemMessageArgs e)
        {
            _router.RouteSystemMessage(e.Cmd, e.Text);
        }

        // ---- Static system-message template table ----

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
    }
}
