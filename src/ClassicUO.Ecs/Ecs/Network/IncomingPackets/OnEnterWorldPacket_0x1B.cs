using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnEnterWorldPacket_0x1B() : IPacket
{
    public byte Id => 0x1B;

    public uint Serial { get; private set; }
    public uint Unused0 { get; private set; }
    public ushort Graphic { get; private set; }
    public (ushort X, ushort Y, sbyte Z) Position { get; private set; }
    public Direction Direction { get; private set; }
    public uint Unused1 { get; private set; }
    public uint Unused2 { get; private set; }
    public byte Unused3 { get; private set; }
    public ushort MapWidth { get; private set; }
    public ushort MapHeight { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Unused0 = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        Position = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), (sbyte)reader.ReadUInt16BE());
        Direction = (Direction)reader.ReadUInt8();
        Unused1 = reader.ReadUInt32BE();
        Unused2 = reader.ReadUInt32BE();
        Unused3 = reader.ReadUInt8();
        MapWidth = reader.ReadUInt16BE();
        MapHeight = reader.ReadUInt16BE();
    }
}
