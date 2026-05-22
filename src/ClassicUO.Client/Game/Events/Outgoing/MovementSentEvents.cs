// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x02 Walk request — direction + sequence + fast-walk key.</summary>
    internal readonly record struct WalkRequestSentArgs(Direction Dir, byte Seq, bool Run, uint FastWalk);

    /// <summary>0x22 Resync request sent to server.</summary>
    internal readonly record struct ResyncSentArgs();

    /// <summary>0xBF/0x33 Multi-boat movement request.</summary>
    internal readonly record struct MultiBoatMoveRequestSentArgs(uint Serial, Direction Dir, byte Speed);
}
