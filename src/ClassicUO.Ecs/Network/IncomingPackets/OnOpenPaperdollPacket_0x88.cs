using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOpenPaperdollPacket_0x88 : IPacket
{
    public byte Id => 0x88;

    public uint Serial { get; private set; }
    public string Title { get; private set; }
    public byte Flags { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Title = reader.ReadASCII(60);
        Flags = reader.ReadUInt8();
    }
}
