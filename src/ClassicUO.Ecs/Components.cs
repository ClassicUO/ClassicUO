using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ClassicUO.Game.Data;
using Microsoft.Xna.Framework;

namespace ClassicUO.Ecs;

internal struct WorldPosition
{
    public ushort X, Y;
    public sbyte Z;

    public readonly void Deconstruct(out ushort x, out ushort y, out sbyte z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector2 WorldToScreen() => Isometric.IsoToScreen(X, Y, Z);
}

internal struct Graphic
{
    public ushort Value;
}

struct Hue
{
    public ushort Value;
}

struct Facing
{
    public ClassicUO.Game.Data.Direction Value;
}

struct NetworkSerial
{
    public uint Value;
}

struct Amount
{
    public int Value;
}

struct ContainedInto;
struct IsContainer;

// Position inside a parent container. Mutually exclusive with WorldPosition:
// items on the ground/in the world carry WorldPosition, items nested in a
// container carry ContainerSlotPosition. Keeping the two as separate types
// stops distance / hit-test code from confusing slot coords with map coords.
struct ContainerSlotPosition
{
    public ushort X;
    public ushort Y;
    public byte GridIndex;
}

struct Hits
{
    public ushort Value, MaxValue;
}

struct Mana
{
    public ushort Value, MaxValue;
}

struct Stamina
{
    public ushort Value, MaxValue;
}

struct Player;

// Mobile notoriety (innocent/criminal/enemy/...) from the world-object packets.
// Drives healthbar name/background hue. Items never carry it.
struct Notoriety
{
    public NotorietyFlag Value;
}

// Display name for a network entity. Player name is also cached in
// GameContext.PlayerName; this component carries names for any mobile whose
// status (0x11) has been requested (e.g. on opening its healthbar).
struct EntityName
{
    public string Value;
}

// Str/Dex/Int stat locks (0=up, 1=down, 2=locked). Client-side state toggled by
// the status gump's lock buttons; persisted on the player so it survives the
// gump reopening. The server doesn't push it in the status packet.
struct StatLocks
{
    public byte Str, Dex, Int;
}

struct PlayerData
{
    public ushort Str, StrMax;
    public ushort Dex, DexMax;
    public ushort Int, IntMax;

    public short StatsCap;
    public byte Followers, FollowersMax;

    public ushort Weight, WeightMax;

    public uint Gold;
    public uint ThithingPoints;
    public ushort Luck;
    public short DamageMin, DamageMax;

    public short PhysicalRes;
    public short FireRes;
    public short ColdRes;
    public short PoisonRes;
    public short EnergyRes;

    public bool IsFemale;

    // Player race (status packet Type >= 5). Drives the racial-abilities book's
    // ability set / icons. 0 (unset) is treated as Human.
    public RaceType Race;

    // AOS extended stats (status packet Type >= 6) — used by the modern status gump.
    public short MaxPhysicalRes, MaxFireRes, MaxColdRes, MaxPoisonRes, MaxEnergyRes;
    public short DefenseChanceInc, MaxDefenseChanceInc;
    public short HitChanceInc, SwingSpeedInc, DamageInc;
    public short LowerReagentCost, SpellDamageInc, FasterCastRecovery, FasterCasting, LowerManaCost;
}

[InlineArray(EquipmentSlots.LayerCount)]
struct EquipmentArray
{
    private ulong _a;
}

struct EquipmentSlots
{
    public const int LayerCount = 54;

    private EquipmentArray _array;

    [UnscopedRef]
    public ref ulong this[Layer layer] => ref _array[(int)layer];

    // Content equality over all layer slots. Packet handlers dirty-check
    // against the stored value before re-Insert so an unchanged equipment
    // payload doesn't bump the component's change tick (which would make
    // Changed<EquipmentSlots> misfire on every benign mobile-update packet).
    public bool ContentEquals(in EquipmentSlots other)
    {
        for (int i = 0; i < LayerCount; i++)
            if (_array[i] != other._array[i])
                return false;
        return true;
    }
}

public struct ScreenPosition
{
    public Vector2 Value;
}

public struct ScreenPositionOffset
{
    public Vector2 Value;
}

internal struct IsStatic;
internal struct IsTile;
internal struct Items;
internal struct Mobiles;
internal struct IsMulti;

internal struct HouseRevision
{
    public uint Value;
}

internal struct CustomMulti;
internal struct NormalMulti;
