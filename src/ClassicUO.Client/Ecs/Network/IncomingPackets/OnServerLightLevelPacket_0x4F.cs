using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnServerLightLevelPacket_0x4F : IPacket
{
    public byte Id => 0x4F;

    public byte Level { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Level = reader.ReadUInt8();
    }
}
