using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOpenBookAltPacket_0xD4 : IPacket
{
    public byte Id => 0xD4;

    public uint Serial { get; private set; }
    public bool FirstFlag { get; private set; }
    public bool IsEditable { get; private set; }
    public ushort PageCount { get; private set; }
    public string Title { get; private set; }
    public string Author { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        FirstFlag = reader.ReadBool();
        IsEditable = reader.ReadBool();
        PageCount = reader.ReadUInt16BE();
        Title = reader.ReadASCII(reader.ReadUInt16BE(), true);
        Author = reader.ReadASCII(reader.ReadUInt16BE(), true);
    }
}
