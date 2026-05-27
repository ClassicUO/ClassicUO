using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnDeleteObjectPacket_0x1D : IPacket
{
    public byte Id => 0x1D;

    public uint Serial { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
    }
}
