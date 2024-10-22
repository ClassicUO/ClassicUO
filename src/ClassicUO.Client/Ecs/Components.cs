using System.Runtime.CompilerServices;
using ClassicUO.Game.Data;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs;

struct WorldPosition : IComponent
{
    public ushort X, Y;
    public sbyte Z;

    public void Deconstruct(out ushort x, out ushort y, out sbyte z)
    {
        x = X;
        y = Y;
        z = Z;
    }
}

struct Graphic : IComponent
{
    public ushort Value;
}

struct Hue : IComponent
{
    public ushort Value;
}

struct Facing : IComponent
{
    public ClassicUO.Game.Data.Direction Value;
}

struct NetworkSerial : IComponent
{
    public uint Value;
}

struct Amount : IComponent
{
    public int Value;
}

struct ContainedInto : IComponent;

struct Hitpoints : IComponent
{
    public ushort Value, MaxValue;
}

struct Mana : IComponent
{
    public ushort Value, MaxValue;
}

struct Stamina : IComponent
{
    public ushort Value, MaxValue;
}

struct Player : IComponent;

struct PlayerData : IComponent
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

struct EquipmentSlots : IComponent
{
    private EquipmentArray _array;

    public ulong this[Layer layer]
    {
        get => _array[(int)layer];
        set => _array[(int)layer] = value;
    }
}

public struct ScreenPositionOffset : IComponent
{
    public Vector2 Value;
}

struct IsStatic : IComponent;
struct IsTile : IComponent;
