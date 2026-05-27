using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnClientVersionPacket_0xBD : IPacket
{
    public byte Id => 0xBD;

    public void Fill(StackDataReader reader)
    {
        // This packet is empty; ensure buffer is fully consumed.
        if (reader.Remaining > 0)
        {
            reader.Skip(reader.Remaining);
        }
    }
}
