// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x72 Query guild position.</summary>
    internal readonly record struct QueryGuildPositionSentArgs();

    /// <summary>0x72 Query party position.</summary>
    internal readonly record struct QueryPartyPositionSentArgs();

    /// <summary>0xBF 0x0C Close status bar gump.</summary>
    internal readonly record struct CloseStatusBarGumpSentArgs(uint Serial);

    /// <summary>0xD7 0x28 Guild menu request.</summary>
    internal readonly record struct GuildMenuRequestSentArgs();

    /// <summary>0xD7 0x32 Quest menu request.</summary>
    internal readonly record struct QuestMenuRequestSentArgs();

    /// <summary>0xD7 0x1C Equip last weapon.</summary>
    internal readonly record struct EquipLastWeaponSentArgs();
}
