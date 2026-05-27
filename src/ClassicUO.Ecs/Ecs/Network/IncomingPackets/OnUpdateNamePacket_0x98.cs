using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateNamePacket_0x98 : IPacket
{
    public byte Id => 0x98;

    public uint Serial { get; private set; }
    public string Name { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Name = reader.ReadASCII();
    }
}
