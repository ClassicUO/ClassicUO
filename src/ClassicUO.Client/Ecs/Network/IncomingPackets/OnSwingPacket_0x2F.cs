using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnSwingPacket_0x2F : IPacket
{
    public byte Id => 0x2F;

    public uint AttackerSerial { get; private set; }
    public uint DefenderSerial { get; private set; }

    public void Fill(StackDataReader reader)
    {
        reader.Skip(1);
        AttackerSerial = reader.ReadUInt32BE();
        DefenderSerial = reader.ReadUInt32BE();
    }
}
