using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnPlayerLightLevelPacket_0x4E : IPacket
{
    public byte Id => 0x4E;

    public uint Serial { get; private set; }
    public byte Level { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Level = reader.ReadUInt8();
    }
}
