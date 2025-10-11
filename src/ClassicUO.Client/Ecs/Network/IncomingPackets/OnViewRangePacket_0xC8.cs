using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnViewRangePacket_0xC8 : IPacket
{
    public byte Id => 0xC8;

    public byte Range { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Range = reader.ReadUInt8();
    }
}
