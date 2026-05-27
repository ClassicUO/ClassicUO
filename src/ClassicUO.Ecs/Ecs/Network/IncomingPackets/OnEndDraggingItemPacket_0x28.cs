using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnEndDraggingItemPacket_0x28 : IPacket
{
    public byte Id => 0x28;

    public void Fill(StackDataReader reader)
    {
        // no payload
    }
}
