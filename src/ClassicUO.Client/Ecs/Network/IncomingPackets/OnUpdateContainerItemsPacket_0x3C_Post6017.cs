using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateContainerItemsPacket_0x3C_Post6017 : IPacket, IUpdateContainerItemsPacket
{
    public byte Id => 0x3C;

    public ushort Count { get; private set; }
    public byte[] ItemsData { get; private set; }
    public bool HasGridIndices => true;

    public void Fill(StackDataReader reader)
    {
        Count = reader.ReadUInt16BE();
        ItemsData = reader.ReadArray(reader.Remaining);
    }
}
