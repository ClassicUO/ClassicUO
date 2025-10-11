using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdatePlayerPacket_0x20 : IPacket
{
    public byte Id => 0x20;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public byte GraphicIncrement { get; private set; }
    public ushort Hue { get; private set; }
    public Flags Flags { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public ushort ServerId { get; private set; }
    public Direction Direction { get; private set; }
    public sbyte Z { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        GraphicIncrement = reader.ReadUInt8();
        Hue = reader.ReadUInt16BE();
        Flags = (Flags)reader.ReadUInt8();
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
        ServerId = reader.ReadUInt16BE();
        Direction = (Direction)reader.ReadUInt8();
        Z = reader.ReadInt8();
    }
}
