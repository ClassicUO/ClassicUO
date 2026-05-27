using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateItemSAPacket_0xF3 : IPacket
{
    public byte Id => 0xF3;

    public byte UpdateType { get; private set; }
    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public byte GraphicIncrement { get; private set; }
    public ushort Amount { get; private set; }
    public ushort Unknown1 { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public sbyte Z { get; private set; }
    public Direction Direction { get; private set; }
    public ushort Hue { get; private set; }
    public Flags Flags { get; private set; }
    public ushort Unknown2 { get; private set; }

    public void Fill(StackDataReader reader)
    {
        reader.Skip(2);
        UpdateType = reader.ReadUInt8();
        Serial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        GraphicIncrement = reader.ReadUInt8();
        Amount = reader.ReadUInt16BE();
        Unknown1 = reader.ReadUInt16BE();
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
        Z = reader.ReadInt8();
        Direction = (Direction)reader.ReadUInt8();
        Hue = reader.ReadUInt16BE();
        Flags = (Flags)reader.ReadUInt8();
        Unknown2 = reader.ReadUInt16BE();
    }
}
