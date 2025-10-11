using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnShowDeathScreenPacket_0x2C : IPacket
{
    public byte Id => 0x2C;

    public byte Action { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Action = reader.ReadUInt8();
    }
}
