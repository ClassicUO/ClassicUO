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

using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Data
{
    internal static class ServerErrorMessages
    {
        private static readonly  Tuple<int, string>[] _loginErrors =
        {
            Tuple.Create(3000007, "Incorrect password"),
            Tuple.Create(3000009, "This character does not exist anymore.  You will have to recreate it."),
            Tuple.Create(3000006, "This character already exists.\nPlaying..."),
            Tuple.Create(3000016, "The client could not attach to the game server. It must have been taken down, please wait a few minutes and try again."),
            Tuple.Create(3000017, "The client could not attach to the game server. It must have been taken down, please wait a few minutes and try again."),
            Tuple.Create(3000012, "Another character from this account is currently online in this world.  You must either log in as that character or wait for it to time out."),
            Tuple.Create(3000013, "An error has occurred in the synchronization between the login servers and this world.  Please close your client and try again."),
            Tuple.Create(3000005, "You have been idle for too long.  If you do not do anything in the next minute, you will be logged out."),
            Tuple.Create(-1, "Could not attach to game server."),
            Tuple.Create(-1,  "Character transfer in progress.")
        };

        private static readonly Tuple<int, string>[] _errorCode =
        {
            Tuple.Create(3000018,"That character password is invalid."),
            Tuple.Create(3000019,"That character does not exist."),
            Tuple.Create(3000020,"That character is being played right now."),
            Tuple.Create(3000021,"That character is not old enough to delete. The character must be 7 days old before it can be deleted."),
            Tuple.Create(3000022,"That character is currently queued for backup and cannot be deleted."),
            Tuple.Create(3000023,"Couldn't carry out your request.")
        };

        private static readonly Tuple<int, string>[] _pickUpErrors =
        {
            Tuple.Create(3000267,"You can not pick that up."),
            Tuple.Create(3000268,"That is too far away."),
            Tuple.Create(3000269,"That is out of sight."),
            Tuple.Create(3000270,"That item does not belong to you.  You'll have to steal it."),
            Tuple.Create(3000271,"You are already holding an item.")
        };

        private static readonly Tuple<int, string>[] _generalErrors =
        {
            Tuple.Create(3000007,"Incorrect name/password."),
            Tuple.Create(3000034,"Someone is already using this account."),
            Tuple.Create(3000035,"Your account has been blocked."),
            Tuple.Create(3000036,"Your account credentials are invalid."),
            Tuple.Create(-1,"Communication problem."),
            Tuple.Create(-1,"The IGR concurrency limit has been met."),
            Tuple.Create(-1,"The IGR time limit has been met."),
            Tuple.Create(-1,"General IGR authentication failure."),
            Tuple.Create(3000037,"Couldn't connect to Ultima Online.  Please try again in a few moments.")
        };

        public static string GetError(byte packetID, byte code)
        {
            ClilocLoader cliloc = ClilocLoader.Instance;
            
            switch (packetID)
            {
                case 0x53:
                    if (code >= 10)
                        code = 9;
                    var t = _loginErrors[code];
                    return cliloc.GetString(t.Item1, t.Item2);
                case 0x85:
                    if (code >= 6)
                        code = 5;
                    t = _errorCode[code];
                    return cliloc.GetString(t.Item1, t.Item2);
                case 0x27:
                    if (code >= 5)
                        code = 4;
                    t = _pickUpErrors[code];
                    return cliloc.GetString(t.Item1, t.Item2);
                case 0x82:
                    if (code >= 9)
                        code = 8;
                    t = _generalErrors[code];
                    return cliloc.GetString(t.Item1, t.Item2);
            }

            return string.Empty;
        }
    }
}