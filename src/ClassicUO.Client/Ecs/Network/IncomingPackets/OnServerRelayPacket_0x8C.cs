using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnServerRelayPacket_0x8C : IPacket
{
    public byte Id => 0x8C;

    public uint Ip { get; private set; }
    public ushort Port { get; private set; }
    public uint Seed { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Ip = reader.ReadUInt32LE();
        Port = reader.ReadUInt16BE();
        Seed = reader.ReadUInt32BE();
    }
}
