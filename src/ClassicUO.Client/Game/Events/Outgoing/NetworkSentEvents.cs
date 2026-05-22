// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x73 Ping sent to server. <see cref="Sequence"/> is the ping sequence byte the client echoes back from a received ping.</summary>
    internal readonly record struct PingSentArgs(byte Sequence);
}
