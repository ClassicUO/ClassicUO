// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Events
{
    internal readonly record struct ItemSpawnedArgs(
        uint Serial,
        ushort Graphic,
        ushort Amount,
        ushort X,
        ushort Y,
        sbyte Z,
        Direction Direction,
        ushort Hue,
        Flags Flags);

    internal readonly record struct ItemRemovedArgs(uint Serial);

    internal readonly record struct ContainerOpenedArgs(uint Serial, ushort Graphic);

    internal readonly record struct ItemEquippedArgs(
        uint Serial,
        ushort Graphic,
        Layer Layer,
        uint ContainerSerial,
        ushort Hue);
}
