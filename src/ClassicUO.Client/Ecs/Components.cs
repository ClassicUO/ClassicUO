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
