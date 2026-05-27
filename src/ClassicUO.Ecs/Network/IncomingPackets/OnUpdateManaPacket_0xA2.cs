using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateManaPacket_0xA2 : IPacket
{
    public byte Id => 0xA2;

    public uint Serial { get; private set; }
    public ushort ManaMax { get; private set; }
    public ushort Mana { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        ManaMax = reader.ReadUInt16BE();
        Mana = reader.ReadUInt16BE();
    }
}
