// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x05 Attack request sent.</summary>
    internal readonly record struct AttackRequestSentArgs(uint Serial);

    /// <summary>0x6C (mode 0) Target an existing object with current cursor.</summary>
    internal readonly record struct TargetObjectSentArgs(
        uint Entity,
        ushort Graphic,
        ushort X,
        ushort Y,
        sbyte Z,
        uint CursorId,
        byte CursorType
    );

    /// <summary>0x6C (mode 1) Target a map XYZ with current cursor.</summary>
    internal readonly record struct TargetXyzSentArgs(
        ushort Graphic,
        ushort X,
        ushort Y,
        sbyte Z,
        uint CursorId,
        byte CursorType
    );

    /// <summary>0x6C Target cancel (escape) sent.</summary>
    internal readonly record struct TargetCancelSentArgs(CursorTarget Type, uint CursorId, byte CursorType);

    /// <summary>0xBF/0x2C Target the selected object (status bar target).</summary>
    internal readonly record struct TargetSelectedObjectSentArgs(uint Serial, uint TargetSerial);

    /// <summary>0x72 Change war mode toggle.</summary>
    internal readonly record struct ChangeWarModeSentArgs(bool State);

    /// <summary>0xD7 Use combat ability slot.</summary>
    internal readonly record struct UseCombatAbilitySentArgs(byte Index);

    /// <summary>0xBF/0x07 Click quest arrow (left vs right).</summary>
    internal readonly record struct ClickQuestArrowSentArgs(bool RightClick);
}
