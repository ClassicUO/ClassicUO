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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.IO;

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

    static class UOChatManager
    {
        public static readonly Dictionary<string, UOChatChannel> Channels = new Dictionary<string, UOChatChannel>();
        public static bool ChatIsEnabled;
        public static string CurrentChannelName = string.Empty;

        private static readonly string[] _messages =
        {
            "You are already ignoring the maximum number of people.",
            "You are already ignoring %1.",
            "You are now ignoring %1.",
            "You are no longer ignoring %1.",
            "You are not ignoring %1.",
            "You are no longer ignoring anyone.",
            "That is not a valid conference name.",
            "There is already a conference of that name.",
            "You must have operator status to do this.",
            "Conference %1 renamed to %2.",
            "You must be in a conference to do this. To join a conference, select one from the Conference menu.",
            "There is no player named '%1'.",
            "There is no conference named '%1'.",
            "That is not the correct password.",
            "%1 has chosen to ignore you. None of your messages to them will get through.",
            "The moderator of this conference has not given you speaking privileges.",
            "You can now receive private messages.",
            "You will no longer receive private messages. Those who send you a message will be notified that you are blocking incoming messages.",
            "You are now showing your character name to any players who inquire with the whois command.",
            "You are no longer showing your character name to any players who inquire with the whois command.",
            "%1 is remaining anonymous.",
            "%1 has chosen to not receive private messages at the moment.",
            "%1 is known in the lands of Britannia as %2.",
            "%1 has been kicked out of the conference.",
            "%1, a conference moderator, has kicked you out of the conference.",
            "You are already in the conference '%1'.",
            "%1 is no longer a conference moderator.",
            "%1 is now a conference moderator.",
            "%1 has removed you from the list of conference moderators.",
            "%1 has made you a conference moderator.",
            "%1 no longer has speaking privileges in this conference.",
            "%1 now has speaking privileges in this conference.",
            "%1, a conference moderator, has removed your speaking privileges for this conference.",
            "%1, a conference moderator, has granted you speaking privileges in this conference.",
            "From now on, everyone in the conference will have speaking privileges by default.",
            "From now on, only moderators will have speaking privileges in this conference by default.",
            "The password to the conference has been changed.",
            "Sorry--the conference named '%1' is full and no more players are allowed in.",
            "You are banning %1 from this conference.",
            "%1, a conference moderator, has banned you from the conference.",
            "You have been banned from this conference.",
        };


        public static string GetMessage(int index)
        {
            if (index < _messages.Length)
                return _messages[index];

            return string.Empty;
        }

        public static void AddChannel(string text, bool haspassword)
        {
            if (!Channels.TryGetValue(text, out var channel))
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
