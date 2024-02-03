#region license

// Copyright (c) 2024, andreakarasho
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

using System.Collections.Generic;
using ClassicUO.Resources;

namespace ClassicUO.Game.Managers
{
    internal sealed class ChatManager
    {
        private readonly World _world;

        public ChatManager(World world) => _world = world;


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