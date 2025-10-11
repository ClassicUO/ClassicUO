using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnPlayMusicPacket_0x6D : IPacket
{
    public byte Id => 0x6D;

    public ushort Index { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Index = reader.ReadUInt16BE();
    }
}
