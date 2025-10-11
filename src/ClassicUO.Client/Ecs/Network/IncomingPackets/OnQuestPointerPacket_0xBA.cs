using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnQuestPointerPacket_0xBA : IPacket
{
    public byte Id => 0xBA;

    public bool Display { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public uint? Serial { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Display = reader.ReadBool();
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();

        if (reader.Remaining >= 4)
        {
            Serial = reader.ReadUInt32BE();
        }
        else
        {
            Serial = null;
        }
    }
}
