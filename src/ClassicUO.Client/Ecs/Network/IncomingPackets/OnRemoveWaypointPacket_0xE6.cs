using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnRemoveWaypointPacket_0xE6 : IPacket
{
    public byte Id => 0xE6;

    public uint Serial { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
    }
}
