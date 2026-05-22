// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.Events
{
    internal readonly record struct WeatherChangedArgs(byte WeatherType, byte Count, byte Temperature);

    internal readonly record struct SeasonChangedArgs(byte Season, byte MusicCue);

    internal readonly record struct LightLevelChangedArgs(uint Serial, byte Level, bool IsPersonal);

    internal readonly record struct ObjectDeletedArgs(uint Serial);

    internal readonly record struct ClientViewRangeChangedArgs(byte Range);

    internal readonly record struct GraphicEffectSpawnedArgs(
        byte EffectType,
        uint Source,
        uint Target,
        ushort Graphic,
        ushort SourceX,
        ushort SourceY,
        sbyte SourceZ,
        ushort TargetX,
        ushort TargetY,
        sbyte TargetZ,
        uint Hue,
        byte Speed,
        byte Duration,
        bool FixedDirection,
        bool DoesExplode,
        byte BlendMode);

    internal readonly record struct SkillNameEntry(int Index, string Name, bool HasButton);

    internal readonly record struct SkillListReceivedArgs(IReadOnlyList<SkillNameEntry> Entries);

    internal readonly record struct SkillEntryUpdate(
        ushort Id,
        ushort RealValue,
        ushort BaseValue,
        byte LockState,
        ushort Cap);

    internal readonly record struct SkillsUpdatedArgs(
        byte UpdateType,
        bool HasCap,
        bool IsSingleUpdate,
        IReadOnlyList<SkillEntryUpdate> Updates);

    internal readonly record struct TargetCursorReceivedArgs(byte CursorType, uint TargetId, byte TargetType);

    internal readonly record struct MultiPlacementReceivedArgs(byte AllowGround, uint TargetId, byte Flags, ushort MultiId, ushort OffsetX, ushort OffsetY, ushort OffsetZ, ushort Hue);

    internal readonly record struct BoatPassenger(uint Serial, ushort X, ushort Y, ushort Z);

    internal readonly record struct BoatMovingReceivedArgs(
        uint Serial,
        byte Speed,
        byte MovingDirection,
        byte FacingDirection,
        ushort X,
        ushort Y,
        ushort Z,
        IReadOnlyList<BoatPassenger> Passengers);

    internal readonly record struct MapDataReceivedArgs(
        uint Serial,
        byte Action,
        ushort PinX,
        ushort PinY,
        byte PlotState);

    internal readonly record struct PathfindingReceivedArgs(ushort X, ushort Y, ushort Z);
}
