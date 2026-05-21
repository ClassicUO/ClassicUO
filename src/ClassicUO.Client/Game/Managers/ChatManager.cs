// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
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
            EventSink.ChannelChatReceived += OnChannelChatReceived;
        }

        public void Unsubscribe()
        {
            EventSink.ChannelChatReceived -= OnChannelChatReceived;
        }

        private void OnChannelChatReceived(ChannelChatReceivedArgs e)
        {
            var p = new StackDataReader(e.Data);
            p.Seek(e.Offset);
            ushort cmd = e.Command;

            switch (cmd)
            {
                case 0x03E8: // create conference
                    p.Skip(4);
                    string channelName = p.ReadUnicodeBE();
                    bool hasPassword = p.ReadUInt16BE() == 0x31;
                    CurrentChannelName = channelName;
                    AddChannel(channelName, hasPassword);

                    UIManager.GetGump<ChatGump>()?.RequestUpdateContents();

                    break;

                case 0x03E9: // destroy conference
                    p.Skip(4);
                    channelName = p.ReadUnicodeBE();
                    RemoveChannel(channelName);

                    UIManager.GetGump<ChatGump>()?.RequestUpdateContents();

                    break;

                case 0x03EB: // display enter username window
                    ChatIsEnabled = ChatStatus.EnabledUserRequest;

                    break;

                case 0x03EC: // close chat
                    Clear();
                    ChatIsEnabled = ChatStatus.Disabled;

                    UIManager.GetGump<ChatGump>()?.Dispose();

                    break;

                case 0x03ED: // username accepted, display chat
                    p.Skip(4);
                    string username = p.ReadUnicodeBE();
                    ChatIsEnabled = ChatStatus.Enabled;
                    NetClient.Socket.Send_ChatJoinCommand("General");

                    break;

                case 0x03EE: // add user
                    p.Skip(4);
                    ushort userType = p.ReadUInt16BE();
                    username = p.ReadUnicodeBE();

                    break;

                case 0x03EF: // remove user
                    p.Skip(4);
                    username = p.ReadUnicodeBE();

                    break;

                case 0x03F0: // clear all players
                    break;

                case 0x03F1: // you have joined a conference
                    p.Skip(4);
                    channelName = p.ReadUnicodeBE();
                    CurrentChannelName = channelName;

                    UIManager.GetGump<ChatGump>()?.UpdateConference();

                    GameActions.Print(
                        _world,
                        string.Format(ResGeneral.YouHaveJoinedThe0Channel, channelName),
                        ProfileManager.CurrentProfile.ChatMessageHue,
                        MessageType.Regular,
                        1
                    );

                    break;

                case 0x03F4:
                    p.Skip(4);
                    channelName = p.ReadUnicodeBE();

                    GameActions.Print(
                        _world,
                        string.Format(ResGeneral.YouHaveLeftThe0Channel, channelName),
                        ProfileManager.CurrentProfile.ChatMessageHue,
                        MessageType.Regular,
                        1
                    );

                    break;

                case 0x0025:
                case 0x0026:
                case 0x0027:
                    p.Skip(4);
                    ushort msgType = p.ReadUInt16BE();
                    username = p.ReadUnicodeBE();
                    string msgSent = p.ReadUnicodeBE();

                    if (!string.IsNullOrEmpty(msgSent))
                    {
                        int idx = msgSent.IndexOf('{');
                        int idxLast = msgSent.IndexOf('}') + 1;

                        if (idxLast > idx && idx > -1)
                        {
                            msgSent = msgSent.Remove(idx, idxLast - idx);
                        }
                    }

                    //Color c = new Color(49, 82, 156, 0);
                    GameActions.Print(
                        _world,
                        $"{username}: {msgSent}",
                        ProfileManager.CurrentProfile.ChatMessageHue,
                        MessageType.Regular,
                        1
                    );

                    break;

                default:
                    if (cmd >= 0x0001 && cmd <= 0x0024 || cmd >= 0x0028 && cmd <= 0x002C)
                    {
                        // TODO: read Chat.enu ?
                        // http://docs.polserver.com/packets/index.php?Packet=0xB2

                        string msg = GetMessage(cmd - 1);

                        if (string.IsNullOrEmpty(msg))
                        {
                            return;
                        }

                        p.Skip(4);
                        string text = p.ReadUnicodeBE();

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

                    break;
            }
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