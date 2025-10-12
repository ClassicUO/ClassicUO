using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateContainerItemsPacket_0x3C : IPacket
{
    public byte Id => 0x3C;

    public ushort Count { get; private set; }
    public byte[] ItemsData { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Count = reader.ReadUInt16BE();
        ItemsData = reader.ReadArray(reader.Remaining);
    }
}
