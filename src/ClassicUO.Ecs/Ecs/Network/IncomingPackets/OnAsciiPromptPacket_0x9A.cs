using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnAsciiPromptPacket_0x9A : IPacket
{
    public byte Id => 0x9A;

    public uint Serial { get; private set; }
    public uint PromptId { get; private set; }
    public uint Type { get; private set; }
    public string Text { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        PromptId = reader.ReadUInt32BE();
        Type = reader.ReadUInt32BE();
        Text = reader.ReadASCII();
    }
}
