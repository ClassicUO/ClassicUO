using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateHitsPacket_0xA1 : IPacket
{
    public byte Id => 0xA1;

    public uint Serial { get; private set; }
    public ushort HitsMax { get; private set; }
    public ushort Hits { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        HitsMax = reader.ReadUInt16BE();
        Hits = reader.ReadUInt16BE();
    }
}
