using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOpenCharacterProfilePacket_0xB8 : IPacket
{
    public byte Id => 0xB8;

    public uint Serial { get; private set; }
    public string Header { get; private set; }
    public string Footer { get; private set; }
    public string Body { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Header = reader.ReadASCII();
        Footer = reader.ReadUnicodeBE();
        Body = reader.ReadUnicodeBE();
    }
}
