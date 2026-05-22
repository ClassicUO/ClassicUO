// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0xBF 0x1E Request house customization data.</summary>
    internal readonly record struct CustomHouseDataRequestSentArgs(uint Serial);

    /// <summary>0xD7 0x02 Custom house: backup.</summary>
    internal readonly record struct CustomHouseBackupSentArgs();

    /// <summary>0xD7 0x03 Custom house: restore.</summary>
    internal readonly record struct CustomHouseRestoreSentArgs();

    /// <summary>0xD7 0x04 Custom house: commit.</summary>
    internal readonly record struct CustomHouseCommitSentArgs();

    /// <summary>0xD7 0x0C Custom house: exit building.</summary>
    internal readonly record struct CustomHouseBuildingExitSentArgs();

    /// <summary>0xD7 0x12 Custom house: go to floor.</summary>
    internal readonly record struct CustomHouseGoToFloorSentArgs(byte Floor);

    /// <summary>0xD7 0x0E Custom house: sync.</summary>
    internal readonly record struct CustomHouseSyncSentArgs();

    /// <summary>0xD7 0x10 Custom house: clear.</summary>
    internal readonly record struct CustomHouseClearSentArgs();

    /// <summary>0xD7 0x1A Custom house: revert.</summary>
    internal readonly record struct CustomHouseRevertSentArgs();

    /// <summary>0xD7 0x0A Custom house: response.</summary>
    internal readonly record struct CustomHouseResponseSentArgs();

    /// <summary>0xD7 0x06 Custom house: add item.</summary>
    internal readonly record struct CustomHouseAddItemSentArgs(ushort Graphic, int X, int Y);

    /// <summary>0xD7 0x05 Custom house: delete item.</summary>
    internal readonly record struct CustomHouseDeleteItemSentArgs(ushort Graphic, int X, int Y, int Z);

    /// <summary>0xD7 0x13 Custom house: add roof.</summary>
    internal readonly record struct CustomHouseAddRoofSentArgs(ushort Graphic, int X, int Y, int Z);

    /// <summary>0xD7 0x14 Custom house: delete roof.</summary>
    internal readonly record struct CustomHouseDeleteRoofSentArgs(ushort Graphic, int X, int Y, int Z);

    /// <summary>0xD7 0x0D Custom house: add stair.</summary>
    internal readonly record struct CustomHouseAddStairSentArgs(ushort Graphic, int X, int Y);
}
