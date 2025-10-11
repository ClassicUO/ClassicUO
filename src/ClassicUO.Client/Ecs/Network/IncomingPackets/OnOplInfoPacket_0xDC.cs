using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOplInfoPacket_0xDC : IPacket
{
    public byte Id => 0xDC;

    public uint Serial { get; private set; }
    public uint Revision { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Revision = reader.ReadUInt32BE();
    }
}
