using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnDamagePacket_0x0B : IPacket
{
    public byte Id => 0x0B;

    public uint Serial { get; private set; }
    public ushort Damage { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Damage = reader.ReadUInt16BE();
    }
}
