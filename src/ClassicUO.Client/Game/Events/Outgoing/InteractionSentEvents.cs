// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x06 Double-click entity (use).</summary>
    internal readonly record struct DoubleClickSentArgs(uint Serial);

    /// <summary>0x09 Single-click entity request.</summary>
    internal readonly record struct ClickRequestSentArgs(uint Serial);

    /// <summary>0x12 Use skill (skill index).</summary>
    internal readonly record struct UseSkillSentArgs(int Index);

    /// <summary>0x98 Name request for entity.</summary>
    internal readonly record struct NameRequestSentArgs(uint Serial);

    /// <summary>0x12 Open door command.</summary>
    internal readonly record struct OpenDoorSentArgs();
}
