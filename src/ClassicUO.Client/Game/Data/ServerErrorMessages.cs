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

using System;
using ClassicUO.Assets;
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