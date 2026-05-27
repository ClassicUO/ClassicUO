using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateSkillsPacket_0x3A : IPacket
{
    internal struct SkillDefinitionEntry
    {
        public bool HasButton;
        public string Name;
    }

    internal struct SkillValueEntry
    {
        public short Id;
        public short RealValue;
        public short BaseValue;
        public Lock Status;
        public short? Cap;
    }

    public byte Id => 0x3A;

    public byte UpdateType { get; private set; }
    public List<SkillDefinitionEntry> Definitions { get; private set; }
    public List<SkillValueEntry> Values { get; private set; }
    public bool HasCap { get; private set; }
    public bool IsSingleUpdate { get; private set; }

    public void Fill(StackDataReader reader)
    {
        UpdateType = reader.ReadUInt8();
        Definitions ??= new List<SkillDefinitionEntry>();
        Values ??= new List<SkillValueEntry>();
        Definitions.Clear();
        Values.Clear();

        if (UpdateType == 0xFE)
        {
            var count = reader.ReadUInt16BE();
            Definitions.Capacity = count;
            for (var i = 0; i < count; ++i)
            {
                var entry = new SkillDefinitionEntry
                {
                    HasButton = reader.ReadBool()
                };

                var nameLen = reader.ReadInt8();
                entry.Name = reader.ReadASCII(nameLen);
                Definitions.Add(entry);
            }

            HasCap = false;
            IsSingleUpdate = false;
            return;
        }

        HasCap = (UpdateType != 0 && UpdateType <= 0x03) || UpdateType == 0xDF;
        IsSingleUpdate = UpdateType == 0xFF || UpdateType == 0xDF;

        while (reader.Remaining > 0)
        {
            var entry = new SkillValueEntry
            {
                Id = reader.ReadInt16BE(),
                RealValue = reader.ReadInt16BE(),
                BaseValue = reader.ReadInt16BE(),
                Status = (Lock)reader.ReadUInt8()
            };

            if (HasCap)
            {
                entry.Cap = reader.ReadInt16BE();
            }

            Values.Add(entry);

            if (IsSingleUpdate)
                break;
        }
    }
}
