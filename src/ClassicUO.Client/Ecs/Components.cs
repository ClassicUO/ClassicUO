namespace ClassicUO.Ecs;

struct WorldPosition
{
    public ushort X, Y;
    public sbyte Z;
}

struct Graphic
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

struct EquippedItem
{
    public ClassicUO.Game.Data.Layer Layer;
}

struct Hitpoints
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

struct IsStatic;
struct IsTile;
