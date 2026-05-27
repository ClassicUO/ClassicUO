using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnMobileAttributesPacket_0x2D : IPacket
{
    public byte Id => 0x2D;

    public uint Serial { get; private set; }
    public ushort HitsMax { get; private set; }
    public ushort Hits { get; private set; }
    public ushort ManaMax { get; private set; }
    public ushort Mana { get; private set; }
    public ushort StaminaMax { get; private set; }
    public ushort Stamina { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        HitsMax = reader.ReadUInt16BE();
        Hits = reader.ReadUInt16BE();
        ManaMax = reader.ReadUInt16BE();
        Mana = reader.ReadUInt16BE();
        StaminaMax = reader.ReadUInt16BE();
        Stamina = reader.ReadUInt16BE();
    }
}
