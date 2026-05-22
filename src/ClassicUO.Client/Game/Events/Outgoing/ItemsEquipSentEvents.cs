// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x07 PickUpRequest sent to server.</summary>
    internal readonly record struct PickUpRequestSentArgs(uint Serial, ushort Count);

    /// <summary>0x08 DropRequest sent to server (post-6.0.1.7 layout with slot byte).</summary>
    internal readonly record struct DropRequestSentArgs(
        uint Serial,
        ushort X,
        ushort Y,
        sbyte Z,
        byte Slot,
        uint Container
    );

    /// <summary>0x08 legacy DropRequest sent to server (pre-6.0.1.7 layout without slot byte).</summary>
    internal readonly record struct DropRequestOldSentArgs(
        uint Serial,
        ushort X,
        ushort Y,
        sbyte Z,
        uint Container
    );

    /// <summary>0x13 EquipRequest sent to server.</summary>
    internal readonly record struct EquipRequestSentArgs(uint Serial, Layer Layer, uint Container);

    /// <summary>0xEC KR equip-macro sent to server. Holds a snapshot copy of the serials.</summary>
    internal readonly record struct EquipMacroKrSentArgs(IReadOnlyList<uint> Serials);

    /// <summary>0xED KR unequip-macro sent to server. Holds a snapshot copy of the layers.</summary>
    internal readonly record struct UnequipMacroKrSentArgs(IReadOnlyList<Layer> Layers);

    /// <summary>0x95 DyeDataResponse sent to server.</summary>
    internal readonly record struct DyeDataResponseSentArgs(uint Serial, ushort Graphic, ushort Hue);

    /// <summary>0x75 RenameRequest sent to server.</summary>
    internal readonly record struct RenameRequestSentArgs(uint Serial, string Name);
}
