using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnSeasonChangePacket_0xBC : IPacket
{
    public byte Id => 0xBC;

    public byte Season { get; private set; }
    public byte Music { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Season = reader.ReadUInt8();
        Music = reader.ReadUInt8();
    }
}
