using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOpenBookPacket_0x93 : IPacket
{
    public byte Id => 0x93;

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
        reader.Skip(1);
        IsEditable = FirstFlag;
        PageCount = reader.ReadUInt16BE();
        // UTF8 to match legacy OpenBook (ReadUTF8 for new books).
        Title = reader.ReadUTF8(60, true);
        Author = reader.ReadUTF8(30, true);
    }
}
