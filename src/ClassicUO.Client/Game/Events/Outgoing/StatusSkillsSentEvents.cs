// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x34 StatusRequest sent to server.</summary>
    internal readonly record struct StatusRequestSentArgs(uint Serial);

    /// <summary>0x34 SkillsRequest sent to server.</summary>
    internal readonly record struct SkillsRequestSentArgs(uint Serial);

    /// <summary>0x3A SkillsStatusRequest sent to server.</summary>
    internal readonly record struct SkillsStatusRequestSentArgs(ushort SkillIndex, byte LockState);

    /// <summary>0xBF StatLockStateRequest sent to server.</summary>
    internal readonly record struct StatLockStateRequestSentArgs(byte Stat, Lock State);

    /// <summary>0x3A SkillStatusChangeRequest sent to server.</summary>
    internal readonly record struct SkillStatusChangeRequestSentArgs(ushort SkillIndex, byte LockState);

    /// <summary>0x9B HelpRequest sent to server.</summary>
    internal readonly record struct HelpRequestSentArgs();

    /// <summary>0xBF (sub 0x10) MegaCliloc request (single serial, legacy) sent to server.</summary>
    internal readonly record struct MegaClilocRequestOldSentArgs(uint Serial);

    /// <summary>0xD6 MegaCliloc batch request sent to server. Holds a snapshot copy of the serials actually written (up to 15).</summary>
    internal readonly record struct MegaClilocRequestSentArgs(IReadOnlyList<uint> Serials);
}
