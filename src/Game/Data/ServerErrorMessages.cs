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
using ClassicUO.Resources;

namespace ClassicUO.Game.Data
{
    internal static class ServerErrorMessages
    {
        private static readonly Tuple<int, string>[] _loginErrors =
        {
            Tuple.Create(3000007, ResErrorMessages.IncorrectPassword),
            Tuple.Create(3000009, ResErrorMessages.CharacterDoesNotExist),
            Tuple.Create(3000006, ResErrorMessages.CharacterAlreadyExists),
            Tuple.Create(3000016, ResErrorMessages.ClientCouldNotAttachToServer),
            Tuple.Create(3000017, ResErrorMessages.ClientCouldNotAttachToServer),
            Tuple.Create(3000012, ResErrorMessages.AnotherCharacterOnline),
            Tuple.Create(3000013, ResErrorMessages.ErrorInSynchronization),
            Tuple.Create(3000005, ResErrorMessages.IdleTooLong),
            Tuple.Create(-1, ResErrorMessages.CouldNotAttachServer),
            Tuple.Create(-1, ResErrorMessages.CharacterTransferInProgress)
        };

        private static readonly Tuple<int, string>[] _errorCode =
        {
            Tuple.Create(3000018, ResErrorMessages.CharacterPasswordInvalid),
            Tuple.Create(3000019, ResErrorMessages.ThatCharacterDoesNotExist),
            Tuple.Create(3000020, ResErrorMessages.ThatCharacterIsBeingPlayed),
            Tuple.Create(3000021, ResErrorMessages.CharacterIsNotOldEnough),
            Tuple.Create(3000022, ResErrorMessages.CharacterIsQueuedForBackup),
            Tuple.Create(3000023, ResErrorMessages.CouldntCarryOutYourRequest)
        };

        private static readonly Tuple<int, string>[] _pickUpErrors =
        {
            Tuple.Create(3000267, ResErrorMessages.YouCanNotPickThatUp),
            Tuple.Create(3000268, ResErrorMessages.ThatIsTooFarAway),
            Tuple.Create(3000269, ResErrorMessages.ThatIsOutOfSight),
            Tuple.Create(3000270, ResErrorMessages.ThatItemDoesNotBelongToYou),
            Tuple.Create(3000271, ResErrorMessages.YouAreAlreadyHoldingAnItem)
        };

        private static readonly Tuple<int, string>[] _generalErrors =
        {
            Tuple.Create(3000007, ResErrorMessages.IncorrectNamePassword),
            Tuple.Create(3000034, ResErrorMessages.SomeoneIsAlreadyUsingThisAccount),
            Tuple.Create(3000035, ResErrorMessages.YourAccountHasBeenBlocked),
            Tuple.Create(3000036, ResErrorMessages.YourAccountCredentialsAreInvalid),
            Tuple.Create(-1, ResErrorMessages.CommunicationProblem),
            Tuple.Create(-1, ResErrorMessages.TheIGRConcurrencyLimitHasBeenMet),
            Tuple.Create(-1, ResErrorMessages.TheIGRTimeLimitHasBeenMet),
            Tuple.Create(-1, ResErrorMessages.GeneralIGRAuthenticationFailure),
            Tuple.Create(3000037, ResErrorMessages.CouldntConnectToUO)
        };

        public static string GetError(byte packetID, byte code)
        {
            ClilocLoader cliloc = ClilocLoader.Instance;

            switch (packetID)
            {
                case 0x53:
                    if (code >= 10)
                    {
                        code = 9;
                    }

                    Tuple<int, string> t = _loginErrors[code];

                    return cliloc.GetString(t.Item1, t.Item2);

                case 0x85:
                    if (code >= 6)
                    {
                        code = 5;
                    }

                    t = _errorCode[code];

                    return cliloc.GetString(t.Item1, t.Item2);

                case 0x27:
                    if (code >= 5)
                    {
                        code = 4;
                    }

                    t = _pickUpErrors[code];

                    return cliloc.GetString(t.Item1, t.Item2);

                case 0x82:
                    if (code >= 9)
                    {
                        code = 8;
                    }

                    t = _generalErrors[code];

                    return cliloc.GetString(t.Item1, t.Item2);
            }

            return string.Empty;
        }
    }
}