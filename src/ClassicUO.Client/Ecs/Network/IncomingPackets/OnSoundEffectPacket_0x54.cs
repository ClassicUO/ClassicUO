using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnSoundEffectPacket_0x54 : IPacket
{
    public byte Id => 0x54;

    public ushort Index { get; private set; }
    public ushort AudioId { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public short Z { get; private set; }

    public void Fill(StackDataReader reader)
    {
        reader.Skip(1);
        Index = reader.ReadUInt16BE();
        AudioId = reader.ReadUInt16BE();
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
        Z = reader.ReadInt16BE();
    }
}
