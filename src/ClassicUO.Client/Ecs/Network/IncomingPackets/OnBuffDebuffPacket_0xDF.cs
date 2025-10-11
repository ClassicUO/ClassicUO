using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnBuffDebuffPacket_0xDF : IPacket
{
    internal struct BuffEntry
    {
        public ushort SourceType;
        public ushort Icon;
        public ushort QueueIndex;
        public ushort Timer;
        public uint TitleCliloc;
        public uint DescriptionCliloc;
        public uint AdditionalCliloc;
        public string Arguments;
        public ushort Arguments2;
        public ushort Arguments3;
    }

    public byte Id => 0xDF;

    public uint Serial { get; private set; }
    public BuffIconType IconType { get; private set; }
    public ushort Count { get; private set; }
    public List<BuffEntry> Entries { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        IconType = (BuffIconType)reader.ReadUInt16BE();
        Count = reader.ReadUInt16BE();

        Entries ??= new List<BuffEntry>();
        Entries.Clear();
        if (Count == 0)
        {
            return;
        }

        Entries.Capacity = Count;
        for (var i = 0; i < Count; ++i)
        {
            var entry = new BuffEntry
            {
                SourceType = reader.ReadUInt16BE()
            };

            reader.Skip(2); // unknown
            entry.Icon = reader.ReadUInt16BE();
            entry.QueueIndex = reader.ReadUInt16BE();
            reader.Skip(4); // unknown
            entry.Timer = reader.ReadUInt16BE();
            reader.Skip(3); // unknown padding

            entry.TitleCliloc = reader.ReadUInt32BE();
            entry.DescriptionCliloc = reader.ReadUInt32BE();
            entry.AdditionalCliloc = reader.ReadUInt32BE();

            var argsLen = reader.ReadUInt16BE();
            string argsPrefix = string.Empty;
            if (argsLen > 0)
            {
                argsPrefix = reader.ReadUnicodeLE(2);
            }

            entry.Arguments = argsPrefix + reader.ReadUnicodeLE();

            argsLen = reader.ReadUInt16BE();
            entry.Arguments2 = argsLen > 0 ? reader.ReadUInt16LE() : (ushort)0;

            argsLen = reader.ReadUInt16BE();
            entry.Arguments3 = argsLen > 0 ? reader.ReadUInt16LE() : (ushort)0;

            Entries.Add(entry);
        }
    }
}
