using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOpenUrlPacket_0xA5 : IPacket
{
    public byte Id => 0xA5;

    public string Url { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Url = reader.ReadASCII();
    }
}
