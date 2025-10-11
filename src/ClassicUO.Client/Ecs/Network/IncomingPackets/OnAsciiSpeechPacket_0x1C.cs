using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnAsciiSpeechPacket_0x1C : IPacket
{
    public byte Id => 0x1C;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public MessageType MessageType { get; private set; }
    public ushort Hue { get; private set; }
    public ushort Font { get; private set; }
    public string Name { get; private set; }
    public string Text { get; private set; }
    public bool IsSystemMessage { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        MessageType = (MessageType)reader.ReadUInt8();
        Hue = reader.ReadUInt16BE();
        Font = reader.ReadUInt16BE();
        Name = reader.ReadASCII(30);

        IsSystemMessage =
            Serial == 0 &&
            Graphic == 0 &&
            MessageType == MessageType.Regular &&
            Font == 0xFFFF &&
            Hue == 0xFFFF &&
            Name.StartsWith("SYSTEM");

        if (!IsSystemMessage)
        {
            Text = reader.ReadASCII();
        }
    }
}
