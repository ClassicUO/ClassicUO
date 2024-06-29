namespace ClassicUO.Ecs;

public struct WorldPosition
{
    public ushort X, Y;
    public sbyte Z;
}

public struct Graphic
{
    public ushort Value;
}

public struct Hue
{
    public ushort Value;
}

public struct NetworkSerial
{
    public uint Value;
}

public struct ContainedInto;

public struct EquippedItem
{
    public byte Layer;
}

public struct Hitpoints
{
    public ushort Value, MaxValue;
}

public struct Mana
{
    public ushort Value, MaxValue;
}

public struct Stamina
{
    public ushort Value, MaxValue;
}

public enum EntityType
{
    Static,
    Land,
    Network,
}