using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUnicodeSpeechPacket_0xAE : IPacket
{
    public byte Id => 0xAE;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public MessageType MessageType { get; private set; }
    public ushort Hue { get; private set; }
    public ushort Font { get; private set; }
    public string Language { get; private set; }
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
        Language = reader.ReadASCII(4);
        Name = reader.ReadASCII(30);

        IsSystemMessage =
            Serial == 0 &&
            Graphic == 0 &&
            MessageType == MessageType.Regular &&
            Font == 0xFFFF &&
            Hue == 0xFFFF &&
            Name.Equals("system", System.StringComparison.InvariantCultureIgnoreCase);

        if (!IsSystemMessage)
        {
            Text = reader.ReadUnicodeBE();
        }
    }
}
