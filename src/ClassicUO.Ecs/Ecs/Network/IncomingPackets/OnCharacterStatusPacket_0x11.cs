using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnCharacterStatusPacket_0x11 : IPacket
{
    public byte Id => 0x11;

    public uint Serial { get; private set; }
    public string Name { get; private set; }
    public ushort Hits { get; private set; }
    public ushort HitsMax { get; private set; }
    public bool CanBeRenamed { get; private set; }
    public byte Type { get; private set; }

    public bool? IsFemale { get; private set; }
    public ushort? Strength { get; private set; }
    public ushort? Dexterity { get; private set; }
    public ushort? Intelligence { get; private set; }
    public ushort? Stamina { get; private set; }
    public ushort? StaminaMax { get; private set; }
    public ushort? Mana { get; private set; }
    public ushort? ManaMax { get; private set; }
    public uint? Gold { get; private set; }
    public short? PhysicalResistance { get; private set; }
    public ushort? Weight { get; private set; }
    public ushort? WeightMax { get; private set; }
    public byte? Race { get; private set; }
    public short? StatsCap { get; private set; }
    public byte? Followers { get; private set; }
    public byte? MaxFollowers { get; private set; }
    public short? FireResistance { get; private set; }
    public short? ColdResistance { get; private set; }
    public short? PoisonResistance { get; private set; }
    public short? EnergyResistance { get; private set; }
    public ushort? Luck { get; private set; }
    public short? DamageMin { get; private set; }
    public short? DamageMax { get; private set; }
    public uint? TithingPoints { get; private set; }
    public short? MaxPhysicalResistance { get; private set; }
    public short? MaxFireResistance { get; private set; }
    public short? MaxColdResistance { get; private set; }
    public short? MaxPoisonResistance { get; private set; }
    public short? MaxEnergyResistance { get; private set; }
    public short? DefenseChanceIncrease { get; private set; }
    public short? MaxDefenseChanceIncrease { get; private set; }
    public short? HitChanceIncrease { get; private set; }
    public short? SwingSpeedIncrease { get; private set; }
    public short? DamageIncrease { get; private set; }
    public short? LowerReagentCost { get; private set; }
    public short? SpellDamageIncrease { get; private set; }
    public short? FasterCastRecovery { get; private set; }
    public short? FasterCasting { get; private set; }
    public short? LowerManaCost { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Name = reader.ReadASCII(30);
        Hits = reader.ReadUInt16BE();
        HitsMax = reader.ReadUInt16BE();
        CanBeRenamed = reader.ReadBool();
        Type = reader.ReadUInt8();

        if (Type > 0)
        {
            IsFemale = reader.ReadBool();
            Strength = reader.ReadUInt16BE();
            Dexterity = reader.ReadUInt16BE();
            Intelligence = reader.ReadUInt16BE();
            Stamina = reader.ReadUInt16BE();
            StaminaMax = reader.ReadUInt16BE();
            Mana = reader.ReadUInt16BE();
            ManaMax = reader.ReadUInt16BE();
            Gold = reader.ReadUInt32BE();
            PhysicalResistance = reader.ReadInt16BE();
            Weight = reader.ReadUInt16BE();

            if (Type >= 5)
            {
                WeightMax = reader.ReadUInt16BE();
                Race = reader.ReadUInt8();
            }

            if (Type >= 3)
            {
                StatsCap = reader.ReadInt16BE();
                Followers = reader.ReadUInt8();
                MaxFollowers = reader.ReadUInt8();
            }

            if (Type >= 4)
            {
                FireResistance = reader.ReadInt16BE();
                ColdResistance = reader.ReadInt16BE();
                PoisonResistance = reader.ReadInt16BE();
                EnergyResistance = reader.ReadInt16BE();
                Luck = reader.ReadUInt16BE();
                DamageMin = reader.ReadInt16BE();
                DamageMax = reader.ReadInt16BE();
                TithingPoints = reader.ReadUInt32BE();
            }

            if (Type >= 6)
            {
                MaxPhysicalResistance = reader.ReadInt16BE();
                MaxFireResistance = reader.ReadInt16BE();
                MaxColdResistance = reader.ReadInt16BE();
                MaxPoisonResistance = reader.ReadInt16BE();
                MaxEnergyResistance = reader.ReadInt16BE();
                DefenseChanceIncrease = reader.ReadInt16BE();
                MaxDefenseChanceIncrease = reader.ReadInt16BE();
                HitChanceIncrease = reader.ReadInt16BE();
                SwingSpeedIncrease = reader.ReadInt16BE();
                DamageIncrease = reader.ReadInt16BE();
                LowerReagentCost = reader.ReadInt16BE();
                SpellDamageIncrease = reader.ReadInt16BE();
                FasterCastRecovery = reader.ReadInt16BE();
                FasterCasting = reader.ReadInt16BE();
                LowerManaCost = reader.ReadInt16BE();
            }
        }
    }
}
