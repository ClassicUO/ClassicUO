using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnDropItemOkPacket_0x29 : IPacket
{
    public byte Id => 0x29;

    public void Fill(StackDataReader reader)
    {
        // no payload
    }
}
