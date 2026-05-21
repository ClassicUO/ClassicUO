// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.Events
{
    internal readonly record struct LoginCompletedArgs;

    internal readonly record struct LoginRejectedArgs(byte PacketId, byte Reason);

    internal readonly record struct PlayerEnteredWorldArgs(
        uint Serial,
        ushort Graphic,
        ushort X,
        ushort Y,
        sbyte Z,
        Direction Direction);

    internal readonly record struct LogoutReceivedArgs(bool CanDisconnect);

    internal readonly record struct ServerListReceivedArgs(byte Flags, IReadOnlyList<ServerListEntry> Servers);

    internal readonly record struct ServerRelayReceivedArgs(uint Ip, ushort Port, uint Seed);

    internal readonly record struct CharacterListUpdatedArgs(IReadOnlyList<string> Characters);

    internal readonly record struct CharacterListReceivedArgs(
        IReadOnlyList<string> Characters,
        IReadOnlyList<CityInfo> Cities,
        uint ClientFlags);

    internal readonly record struct LoginDelayReceivedArgs(byte Delay);

    internal readonly record struct ClientVersionRequestedArgs;

    internal readonly record struct LockedFeaturesEnabledArgs(uint Flags);
}
