using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnDenyWalkPacket_0x21 : IPacket
{
    public byte Id => 0x21;

    public byte Sequence { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public Direction Direction { get; private set; }
    public sbyte Z { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Sequence = reader.ReadUInt8();
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
        Direction = (Direction)reader.ReadUInt8();
        Z = reader.ReadInt8();
    }
}
