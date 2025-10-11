using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnNewCharacterAnimationPacket_0xE2 : IPacket
{
    public byte Id => 0xE2;

    public uint Serial { get; private set; }
    public ushort Type { get; private set; }
    public ushort Action { get; private set; }
    public byte Mode { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Type = reader.ReadUInt16BE();
        Action = reader.ReadUInt16BE();
        Mode = reader.ReadUInt8();
    }
}
