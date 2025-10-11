using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnPingPacket_0x73 : IPacket
{
    public byte Id => 0x73;

    public byte Sequence { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Sequence = reader.ReadUInt8();
    }
}
