using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnShowDeathActionPacket_0xAF : IPacket
{
    public byte Id => 0xAF;

    public uint Serial { get; private set; }
    public uint CorpseSerial { get; private set; }
    public uint Action { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        CorpseSerial = reader.ReadUInt32BE();
        Action = reader.ReadUInt32BE();
    }
}
