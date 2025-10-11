using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnSellListPacket_0x9E : IPacket
{
    internal struct SellEntry
    {
        public uint ItemSerial;
        public ushort Graphic;
        public ushort Hue;
        public ushort Amount;
        public ushort Price;
        public string Name;
    }

    public byte Id => 0x9E;

    public uint Serial { get; private set; }
    public ushort Count { get; private set; }
    public List<SellEntry> Entries { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Count = reader.ReadUInt16BE();

        Entries ??= new List<SellEntry>();
        Entries.Clear();
        Entries.Capacity = Count;
        for (var i = 0; i < Count; ++i)
        {
            var entry = new SellEntry
            {
                ItemSerial = reader.ReadUInt32BE(),
                Graphic = reader.ReadUInt16BE(),
                Hue = reader.ReadUInt16BE(),
                Amount = reader.ReadUInt16BE(),
                Price = reader.ReadUInt16BE()
            };

            var nameLen = reader.ReadUInt16BE();
            entry.Name = reader.ReadASCII(nameLen);

            Entries.Add(entry);
        }
    }
}
