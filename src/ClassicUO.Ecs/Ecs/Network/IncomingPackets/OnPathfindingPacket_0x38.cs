using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnPathfindingPacket_0x38 : IPacket
{
    public byte Id => 0x38;

    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public ushort Z { get; private set; }

    public void Fill(StackDataReader reader)
    {
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
        Z = reader.ReadUInt16BE();
    }
}
