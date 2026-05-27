using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOpenContainerPacket_0x24 : IPacket
{
    public byte Id => 0x24;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
    }
}
