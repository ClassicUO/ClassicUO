using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnLoginCompletePacket_0x55 : IPacket
{
    public byte Id => 0x55;

    public void Fill(StackDataReader reader)
    {
        if (reader.Remaining > 0)
        {
            reader.Skip(reader.Remaining);
        }
    }
}
