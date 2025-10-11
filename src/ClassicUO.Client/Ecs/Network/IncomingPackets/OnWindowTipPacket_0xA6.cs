using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnWindowTipPacket_0xA6 : IPacket
{
    public byte Id => 0xA6;

    public byte Flags { get; private set; }
    public uint Serial { get; private set; }
    public ushort TextLength { get; private set; }
    public string Text { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Flags = reader.ReadUInt8();
        Serial = reader.ReadUInt32BE();
        TextLength = reader.ReadUInt16BE();
        Text = reader.ReadASCII(TextLength);
    }
}
