// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events
{
    internal readonly record struct ConnectedArgs;

    internal readonly record struct DisconnectedArgs(string Reason);

    internal readonly record struct PingReceivedArgs(byte Sequence);
}
