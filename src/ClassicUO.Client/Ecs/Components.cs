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

    public readonly Vector2 ToIso() => Isometric.IsoToScreen(X, Y, Z);
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
}

[InlineArray(0x1D + 1)]
struct EquipmentArray
{
    private ulong _a;
}

struct EquipmentSlots
{
    private EquipmentArray _array;

    [UnscopedRef]
    public ref ulong this[Layer layer] => ref _array[(int)layer];
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
