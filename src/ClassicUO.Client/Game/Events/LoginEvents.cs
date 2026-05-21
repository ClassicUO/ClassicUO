// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Events
{
    internal readonly record struct LoginCompletedArgs;

    internal readonly record struct LoginRejectedArgs(byte Reason);

    internal readonly record struct PlayerEnteredWorldArgs(
        uint Serial,
        ushort Graphic,
        ushort X,
        ushort Y,
        sbyte Z,
        Direction Direction);

    internal readonly record struct LogoutReceivedArgs(bool CanDisconnect);

    internal readonly record struct ServerListReceivedArgs;

    internal readonly record struct ServerRelayReceivedArgs;

    internal readonly record struct CharacterListUpdatedArgs;

    internal readonly record struct CharacterListReceivedArgs;

    internal readonly record struct LoginDelayReceivedArgs;

    internal readonly record struct ClientVersionRequestedArgs;

    internal readonly record struct LockedFeaturesEnabledArgs(uint Flags);
}
