using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnColorPickerPacket_0x95 : IPacket
{
    public byte Id => 0x95;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        reader.Skip(2); // unknown
        Graphic = reader.ReadUInt16BE();
    }
}
