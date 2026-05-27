using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnClilocMessageAffixPacket_0xCC : IPacket
{
    public byte Id => 0xCC;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public MessageType MessageType { get; private set; }
    public ushort Hue { get; private set; }
    public ushort Font { get; private set; }
    public uint Cliloc { get; private set; }
    public AffixType AffixType { get; private set; }
    public string Name { get; private set; }
    public string Affix { get; private set; }
    public string Arguments { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        MessageType = (MessageType)reader.ReadUInt8();
        Hue = reader.ReadUInt16BE();
        Font = reader.ReadUInt16BE();
        Cliloc = reader.ReadUInt32BE();
        AffixType = (AffixType)reader.ReadUInt8();
        Name = reader.ReadASCII(30);
        Affix = reader.ReadASCII();
        Arguments = reader.ReadUnicodeBE();
    }
}
