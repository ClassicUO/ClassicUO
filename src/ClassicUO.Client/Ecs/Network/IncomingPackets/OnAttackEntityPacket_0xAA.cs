using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnAttackEntityPacket_0xAA : IPacket
{
    public byte Id => 0xAA;

    public uint Serial { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
    }
}
