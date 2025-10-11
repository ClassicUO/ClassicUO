using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnMegaClilocPacket_0xD6 : IPacket
{
    internal struct ClilocEntry
    {
        public int Cliloc;
        public string Argument;
    }

    public byte Id => 0xD6;

    public ushort Unknown { get; private set; }
    public uint Serial { get; private set; }
    public uint Revision { get; private set; }
    public List<ClilocEntry> Entries { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Unknown = reader.ReadUInt16BE();
        Serial = reader.ReadUInt32BE();
        reader.Skip(2);
        Revision = reader.ReadUInt32BE();

        Entries ??= new List<ClilocEntry>();
        Entries.Clear();
        int cliloc;
        while ((cliloc = reader.ReadInt32BE()) != 0)
        {
            var len = reader.ReadUInt16BE();
            var argument = len > 0 ? reader.ReadUnicodeLE(len / 2) : string.Empty;
            Entries.Add(new ClilocEntry { Cliloc = cliloc, Argument = argument });
        }
    }
}
