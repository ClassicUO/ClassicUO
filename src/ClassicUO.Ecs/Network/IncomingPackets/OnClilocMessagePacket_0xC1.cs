using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnClilocMessagePacket_0xC1 : IPacket
{
    public byte Id => 0xC1;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public MessageType MessageType { get; private set; }
    public ushort Hue { get; private set; }
    public ushort Font { get; private set; }
    public uint Cliloc { get; private set; }
    public string Name { get; private set; }
    public string Arguments { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        MessageType = (MessageType)reader.ReadUInt8();
        Hue = reader.ReadUInt16BE();
        Font = reader.ReadUInt16BE();
        Cliloc = reader.ReadUInt32BE();
        Name = reader.ReadASCII(30);
        Arguments = reader.ReadUnicodeLE(reader.Remaining / 2);
    }
}
