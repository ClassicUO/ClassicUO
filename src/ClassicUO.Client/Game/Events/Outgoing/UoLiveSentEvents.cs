// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x3F UOLive block hash response. <see cref="Checksums"/> is a copy of the original span.</summary>
    internal readonly record struct UoLiveHashResponseSentArgs(uint Block, byte MapIndex, ushort[] Checksums);
}
