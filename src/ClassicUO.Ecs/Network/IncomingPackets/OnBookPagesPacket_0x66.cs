using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnBookPagesPacket_0x66 : IPacket
{
    internal struct BookPage
    {
        public ushort Number;
        public List<string> Lines;
    }

    public byte Id => 0x66;

    public uint Serial { get; private set; }
    public ushort PageCount { get; private set; }
    public List<BookPage> Pages { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        PageCount = reader.ReadUInt16BE();

        Pages ??= new List<BookPage>();
        Pages.Clear();
        Pages.Capacity = PageCount;
        for (var i = 0; i < PageCount; ++i)
        {
            var page = new BookPage
            {
                Number = reader.ReadUInt16BE(),
                Lines = new List<string>()
            };

            var linesCount = reader.ReadUInt16BE();
            page.Lines.Capacity = linesCount;
            for (var line = 0; line < linesCount; ++line)
            {
                // New books (CV > 2.0, the only versions ECS targets) stream
                // page lines as UTF8 — legacy BookData reads ReadUTF8 and the
                // ECS outgoing 0x66 writes UTF8. ReadASCII garbled any non-ASCII.
                page.Lines.Add(reader.ReadUTF8(true));
            }

            Pages.Add(page);
        }
    }
}
