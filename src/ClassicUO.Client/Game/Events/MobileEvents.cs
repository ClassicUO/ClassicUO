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

    internal readonly record struct MobileUpdatedArgs(
        uint Serial,
        ushort Graphic,
        ushort X,
        ushort Y,
        sbyte Z,
        Direction Direction,
        ushort Hue,
        Flags Flags,
        NotorietyFlag Notoriety);

    internal readonly record struct PlayerUpdatedArgs(
        uint Serial,
        ushort Graphic,
        byte GraphicIncrement,
        ushort Hue,
        Flags Flags,
        ushort X,
        ushort Y,
        sbyte Z,
        ushort ServerId,
        Direction Direction);

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

    internal readonly record struct WalkDeniedArgs(
        byte Sequence,
        ushort X,
        ushort Y,
        sbyte Z,
        Direction Direction);

    internal readonly record struct WalkConfirmedArgs(byte Sequence, byte Notoriety);

    internal readonly record struct PlayerMovedArgs(Direction Direction, bool IsRunning);

    internal readonly record struct MobileNameChangedArgs(uint Serial, string Name);

    internal readonly record struct HealthBarStateChangedArgs(uint Serial, ushort StateType, bool Enabled);

    internal readonly record struct BuffAppliedArgs(uint Serial, ushort IconType, ushort IconId, ushort Timer, string Text);

    internal readonly record struct BuffRemovedArgs(uint Serial, ushort IconType);

    internal readonly record struct CharacterAnimationArgs(
        uint Serial,
        ushort Action,
        ushort FrameCount,
        ushort RepeatCount,
        bool Forward,
        bool Repeat,
        byte Delay);

    internal readonly record struct NewCharacterAnimationArgs(
        uint Serial,
        ushort Type,
        ushort Action,
        byte Mode);

    internal readonly record struct MobileStatusUpdatedArgs(uint Serial, byte Status);
}
