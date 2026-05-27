using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnHealthBarStatusPacket_0x16 : IPacket
{
    internal struct AttributeEntry
    {
        public ushort Type;
        public bool Enabled;
    }

    public byte Id => 0x16;

    public uint Serial { get; private set; }
    public ushort Count { get; private set; }
    public List<AttributeEntry> Attributes { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Count = reader.ReadUInt16BE();

        Attributes ??= new List<AttributeEntry>();
        Attributes.Clear();
        Attributes.Capacity = Count;
        for (var i = 0; i < Count; ++i)
        {
            Attributes.Add(new AttributeEntry
            {
                Type = reader.ReadUInt16BE(),
                Enabled = reader.ReadBool()
            });
        }
    }
}
