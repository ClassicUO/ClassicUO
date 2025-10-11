using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnWarmodePacket_0x72 : IPacket
{
    public byte Id => 0x72;

    public bool WarmodeEnabled { get; private set; }

    public void Fill(StackDataReader reader)
    {
        WarmodeEnabled = reader.ReadBool();
    }
}
