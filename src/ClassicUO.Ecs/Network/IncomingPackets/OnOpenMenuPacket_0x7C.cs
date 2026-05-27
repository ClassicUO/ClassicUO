using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnOpenMenuPacket_0x7C : IPacket
{
    internal struct MenuEntry
    {
        public ushort MenuId;
        public ushort Hue;
        public string Response;
    }

    public byte Id => 0x7C;

    public uint Serial { get; private set; }
    public ushort MenuId { get; private set; }
    public string Name { get; private set; }
    public byte EntryCount { get; private set; }
    public List<MenuEntry> Entries { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        MenuId = reader.ReadUInt16BE();
        Name = reader.ReadASCII(reader.ReadUInt8());
        EntryCount = reader.ReadUInt8();

        Entries ??= new List<MenuEntry>();
        Entries.Clear();
        Entries.Capacity = EntryCount;
        for (var i = 0; i < EntryCount; ++i)
        {
            var entry = new MenuEntry
            {
                MenuId = reader.ReadUInt16BE(),
                Hue = reader.ReadUInt16BE()
            };

            var responseLen = reader.ReadUInt8();
            entry.Response = reader.ReadASCII(responseLen);
            Entries.Add(entry);
        }
    }
}
