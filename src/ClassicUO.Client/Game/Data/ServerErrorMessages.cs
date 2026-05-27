// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Assets;
using ClassicUO.Resources;

namespace ClassicUO.Game.Data
{
    internal static class ServerErrorMessages
    {
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

        private static string GetLoginError(ClilocLoader cliloc, byte code, (int min, int max) delay)
        {
            return code switch
            {
                0 => cliloc.GetString(3000007, ResErrorMessages.IncorrectPassword),
                1 => cliloc.GetString(3000009, ResErrorMessages.CharacterDoesNotExist),
                2 => cliloc.GetString(3000006, ResErrorMessages.CharacterAlreadyExists),
                3 => cliloc.GetString(3000016, ResErrorMessages.ClientCouldNotAttachToServer),
                4 => cliloc.GetString(3000017, ResErrorMessages.ClientCouldNotAttachToServer),
                5 => cliloc.GetString(3000012, ResErrorMessages.AnotherCharacterOnline),
                6 => cliloc.GetString(3000013, ResErrorMessages.ErrorInSynchronization),
                7 => cliloc.GetString(3000005, ResErrorMessages.IdleTooLong),
                8 => cliloc.GetString(-1, ResErrorMessages.CouldNotAttachServer),
                9 => cliloc.GetString(-1, ResErrorMessages.CharacterTransferInProgress),
                10 => cliloc.GetString(-1, ResErrorMessages.NameIsInvalid),
                13 => cliloc.Translate(1161061, $"{delay.min}\t{delay.max}"),
                14 => cliloc.Translate(1161062, $"{delay.min}\t{delay.max}"),
                _ => $"Unkown error #{code}"
            };
        }

        public static string GetError(byte packetID, byte code, (int min, int max) delay = default)
        {
            var cliloc = Client.Game.UO.FileManager.Clilocs;

            switch (packetID)
            {
                case 0x53:
                    return GetLoginError(cliloc, code, delay);

                case 0x85:
                    if (code >= 6)
                    {
                        code = 5;
                    }

                    var t = _errorCode[code];

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