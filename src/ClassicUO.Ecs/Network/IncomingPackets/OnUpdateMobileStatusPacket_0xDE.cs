using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateMobileStatusPacket_0xDE : IPacket
{
    public byte Id => 0xDE;

    public uint Serial { get; private set; }
    public byte Status { get; private set; }
    public uint? OpponentSerial { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Status = reader.ReadUInt8();
        OpponentSerial = Status == 1 ? reader.ReadUInt32BE() : null;
    }
}
