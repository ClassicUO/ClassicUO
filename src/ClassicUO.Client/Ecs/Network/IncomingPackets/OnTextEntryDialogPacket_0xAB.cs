using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnTextEntryDialogPacket_0xAB : IPacket
{
    public byte Id => 0xAB;

    public uint Serial { get; private set; }
    public byte ParentId { get; private set; }
    public byte ButtonId { get; private set; }
    public ushort TextLength { get; private set; }
    public string Text { get; private set; }
    public bool ShowCancel { get; private set; }
    public byte Variant { get; private set; }
    public uint MaxLength { get; private set; }
    public ushort DescriptionLength { get; private set; }
    public string Description { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        ParentId = reader.ReadUInt8();
        ButtonId = reader.ReadUInt8();
        TextLength = reader.ReadUInt16BE();
        Text = reader.ReadASCII(TextLength);
        ShowCancel = reader.ReadBool();
        Variant = reader.ReadUInt8();
        MaxLength = reader.ReadUInt32BE();
        DescriptionLength = reader.ReadUInt16BE();
        Description = reader.ReadASCII(DescriptionLength);
    }
}
