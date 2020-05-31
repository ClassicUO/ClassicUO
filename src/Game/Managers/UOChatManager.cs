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

using ClassicUO.Resources;
using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    sealed class UOChatChannel
    {
        public UOChatChannel(string name, bool haspassword)
        {
            Name = name;
            HasPassword = haspassword;
        }

        public readonly string Name;
        public readonly bool HasPassword;
    }

    enum CHAT_STATUS : byte
    {
        DISABLED,
        ENABLED,
        ENABLED_USER_REQUEST
    }

    static class UOChatManager
    {
        public static readonly Dictionary<string, UOChatChannel> Channels = new Dictionary<string, UOChatChannel>();
        public static CHAT_STATUS ChatIsEnabled;
        public static string CurrentChannelName = string.Empty;

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
            ResGeneral.YouHaveBeenBanned,
        };


        public static string GetMessage(int index)
        {
            if (index < _messages.Length)
                return _messages[index];

            return string.Empty;
        }

        public static void AddChannel(string text, bool haspassword)
        {
            if (!Channels.TryGetValue(text, out UOChatChannel channel))
            {
                channel = new UOChatChannel(text, haspassword);
                Channels[text] = channel;
            }
        }

        public static void RemoveChannel(string name)
        {
            if (Channels.ContainsKey(name))
            {
                Channels.Remove(name);
            }
        }

        public static void Clear()
        {
            Channels.Clear();
        }

        //static UOChatManager()
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
