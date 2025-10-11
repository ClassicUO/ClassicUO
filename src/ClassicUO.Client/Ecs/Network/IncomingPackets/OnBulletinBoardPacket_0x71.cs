using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnBulletinBoardPacket_0x71 : IPacket
{
    public byte Id => 0x71;

    public byte Type { get; private set; }
    public uint BoardSerial { get; private set; }

    // Type 1 data
    public uint? MessageSerial { get; private set; }
    public uint? MessageParentSerial { get; private set; }
    public string MessagePreview { get; private set; }

    // Type 2 data
    public string Author { get; private set; }
    public string Subject { get; private set; }
    public string DateTime { get; private set; }
    public List<string> Lines { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Type = reader.ReadUInt8();
        BoardSerial = reader.ReadUInt32BE();

        MessageSerial = null;
        MessageParentSerial = null;
        MessagePreview = string.Empty;
        Author = string.Empty;
        Subject = string.Empty;
        DateTime = string.Empty;
        Lines ??= new List<string>();
        Lines.Clear();

        switch (Type)
        {
            case 1:
                MessageSerial = reader.ReadUInt32BE();
                MessageParentSerial = reader.ReadUInt32BE();

                var partLen = reader.ReadUInt8();
                var preview = reader.ReadASCII(partLen, true);
                partLen = reader.ReadUInt8();
                preview += " " + reader.ReadASCII(partLen, true);
                partLen = reader.ReadUInt8();
                preview += " " + reader.ReadASCII(partLen, true);
                MessagePreview = preview.Trim();
                break;

            case 2:
                var len = reader.ReadUInt8();
                Author = reader.ReadASCII(len, true);
                len = reader.ReadUInt8();
                Subject = reader.ReadASCII(len, true);
                len = reader.ReadUInt8();
                DateTime = reader.ReadASCII(len, true);

                reader.Skip(4); // message ID
                var attachments = reader.ReadUInt8();
                if (attachments > 0)
                {
                    reader.Skip(attachments * 4);
                }

                var lineCount = reader.ReadUInt8();
                Lines.Capacity = lineCount;
                for (var i = 0; i < lineCount; ++i)
                {
                    var lineLen = reader.ReadUInt8();
                    Lines.Add(reader.ReadASCII(lineLen, true));
                }
                break;
        }
    }
}
