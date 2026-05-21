// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Events
{
    internal readonly record struct MobileSpawnedArgs(
        uint Serial,
        ushort Graphic,
        ushort X,
        ushort Y,
        sbyte Z,
        Direction Direction,
        ushort Hue,
        Flags Flags,
        NotorietyFlag Notoriety);

    internal readonly record struct MobileMovedArgs(
        uint Serial,
        ushort X,
        ushort Y,
        sbyte Z,
        Direction Direction);

    internal readonly record struct MobileRemovedArgs(uint Serial);

    internal readonly record struct MobileAttributesUpdatedArgs(
        uint Serial,
        ushort HitsMax,
        ushort Hits,
        ushort ManaMax,
        ushort Mana,
        ushort StaminaMax,
        ushort Stamina);

    internal readonly record struct HitpointsUpdatedArgs(uint Serial, ushort HitsMax, ushort Hits);
    internal readonly record struct ManaUpdatedArgs(uint Serial, ushort ManaMax, ushort Mana);
    internal readonly record struct StaminaUpdatedArgs(uint Serial, ushort StaminaMax, ushort Stamina);
}
