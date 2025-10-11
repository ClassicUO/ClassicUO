using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnBuyListPacket_0x74 : IPacket
{
    internal struct BuyEntry
    {
        public uint Price;
        public string Name;
    }

    public byte Id => 0x74;

    public uint ContainerSerial { get; private set; }
    public byte Count { get; private set; }
    public List<BuyEntry> Entries { get; private set; }

    public void Fill(StackDataReader reader)
    {
        ContainerSerial = reader.ReadUInt32BE();
        Count = reader.ReadUInt8();

        Entries ??= new List<BuyEntry>();
        Entries.Clear();
        Entries.Capacity = Count;
        for (var i = 0; i < Count; ++i)
        {
            var entry = new BuyEntry
            {
                Price = reader.ReadUInt32BE()
            };

            var nameLen = reader.ReadUInt8();
            entry.Name = reader.ReadASCII(nameLen);

            Entries.Add(entry);
        }
    }
}
