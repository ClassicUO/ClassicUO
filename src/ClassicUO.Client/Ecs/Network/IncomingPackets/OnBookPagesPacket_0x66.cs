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
                page.Lines.Add(reader.ReadASCII());
            }

            Pages.Add(page);
        }
    }
}
