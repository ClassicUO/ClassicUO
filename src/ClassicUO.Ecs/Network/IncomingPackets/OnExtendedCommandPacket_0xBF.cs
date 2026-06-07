using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnExtendedCommandPacket_0xBF : IPacket
{
    internal struct DisplayEquipInfoEntry
    {
        public uint AttributeId;
        public short Charges;
    }

    internal struct DisplayEquipInfoData
    {
        public uint ItemSerial;
        public uint Cliloc;
        public uint Sentinel1;
        public string OwnerName;
        public uint Sentinel2;
        public List<DisplayEquipInfoEntry> Entries;
    }

    internal struct SpellbookContentData
    {
        public uint Serial;
        public ushort Graphic;
        public ushort Type;
        public uint[] SpellBitfields;
    }

    internal struct HouseCustomizationData
    {
        public uint Serial;
        public byte Type;
        public ushort Graphic;
        public ushort X;
        public ushort Y;
        public sbyte Z;
    }

    internal struct StatueAnimationData
    {
        public ushort Serial;
        public byte AnimationId;
        public byte FrameCount;
    }

    // One entry of a 0x14 "display popup/context menu". Flags: 0x01 disabled
    // (greyed), 0x02 arrow/continuation, 0x20 carries a replaced hue.
    internal struct PopupMenuItemData
    {
        public int Cliloc;
        public ushort Index;
        public ushort Hue;        // 0xFFFF white, 0x0386 when disabled
        public ushort ReplacedHue;
        public ushort Flags;
    }

    internal struct PopupMenuInfo
    {
        public uint Serial;
        public PopupMenuItemData[] Items;
    }

    public byte Id => 0xBF;

    public ushort Command { get; private set; }

    public uint[] FastWalkKeys { get; private set; }
    public uint? FastWalkNewKey { get; private set; }
    public uint? ClosedGumpSerial { get; private set; }
    public int? ClosedGumpButton { get; private set; }
    public byte? MapIndex { get; private set; }
    public uint? StatusBarSerial { get; private set; }
    public DisplayEquipInfoData? DisplayEquipInfo { get; private set; }
    public uint? ClosedLocalGumpType { get; private set; }
    public uint? ClosedLocalGumpSerial { get; private set; }
    public byte? StatsVersion { get; private set; }
    public uint? StatsSerial { get; private set; }
    public SpellbookContentData? SpellbookContent { get; private set; }
    public uint? HouseRevisionSerial { get; private set; }
    public uint? HouseRevision { get; private set; }
    public HouseCustomizationData? HouseCustomization { get; private set; }
    public uint? DamageSerial { get; private set; }
    public byte? DamageAmount { get; private set; }
    public ushort? SpellIconSpell { get; private set; }
    public bool? SpellIconActive { get; private set; }
    // Subcommand 0x21: server cleared all queued special moves.
    public bool ClearAbilities { get; private set; }
    public CharacterSpeedType? CharacterSpeedMode { get; private set; }
    public bool? IsFemale { get; private set; }
    public RaceType? Race { get; private set; }
    public StatueAnimationData? StatueAnimation { get; private set; }
    public PopupMenuInfo? PopupMenu { get; private set; }

    // Party (subcommand 0x06). PartyCode: 1 add, 2 remove, 3/4 message, 7 invite.
    public byte? PartyCode { get; private set; }
    public byte PartyCount { get; private set; }
    public uint[] PartyMembers { get; private set; }
    public uint? PartyRemovedSerial { get; private set; }
    public uint? PartyMessageSerial { get; private set; }
    public string PartyMessageText { get; private set; }
    public uint? PartyInviter { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Command = reader.ReadUInt16BE();

        // reset mutable state
        FastWalkKeys = Array.Empty<uint>();
        FastWalkNewKey = null;
        ClosedGumpSerial = null;
        ClosedGumpButton = null;
        MapIndex = null;
        StatusBarSerial = null;
        DisplayEquipInfo = null;
        ClosedLocalGumpType = null;
        ClosedLocalGumpSerial = null;
        StatsVersion = null;
        StatsSerial = null;
        SpellbookContent = null;
        HouseRevisionSerial = null;
        HouseRevision = null;
        HouseCustomization = null;
        DamageSerial = null;
        DamageAmount = null;
        SpellIconSpell = null;
        SpellIconActive = null;
        ClearAbilities = false;
        CharacterSpeedMode = null;
        IsFemale = null;
        Race = null;
        StatueAnimation = null;
        PopupMenu = null;
        PartyCode = null;
        PartyCount = 0;
        PartyMembers = Array.Empty<uint>();
        PartyRemovedSerial = null;
        PartyMessageSerial = null;
        PartyMessageText = null;
        PartyInviter = null;

        switch (Command)
        {
            case 1:
                FastWalkKeys = new uint[6];
                for (var i = 0; i < FastWalkKeys.Length; ++i)
                {
                    FastWalkKeys[i] = reader.ReadUInt32BE();
                }
                break;

            case 2:
                FastWalkNewKey = reader.ReadUInt32BE();
                break;

            case 4:
                ClosedGumpSerial = reader.ReadUInt32BE();
                ClosedGumpButton = reader.ReadInt32BE();
                break;

            case 6:
                {
                    byte code = reader.ReadUInt8();
                    PartyCode = code;

                    switch (code)
                    {
                        case 1:
                        case 2:
                            byte count = reader.ReadUInt8();
                            PartyCount = count;
                            // count <= 1 means disband — no member list follows.
                            if (count > 1)
                            {
                                // Remove (code 2) prefixes the dropped serial; add
                                // (code 1) does not. The member list (the new party
                                // roster, leader first) follows either way.
                                if (code == 2)
                                    PartyRemovedSerial = reader.ReadUInt32BE();

                                var members = new uint[count];
                                for (int i = 0; i < count; i++)
                                    members[i] = reader.ReadUInt32BE();
                                PartyMembers = members;
                            }
                            break;

                        case 3:
                        case 4:
                            PartyMessageSerial = reader.ReadUInt32BE();
                            PartyMessageText = reader.ReadUnicodeBE();
                            break;

                        case 7:
                            PartyInviter = reader.ReadUInt32BE();
                            break;
                    }
                }
                break;

            case 8:
                MapIndex = reader.ReadUInt8();
                break;

            case 0x0C:
                StatusBarSerial = reader.ReadUInt32BE();
                break;

            case 0x10:
                {
                    var info = new DisplayEquipInfoData
                    {
                        ItemSerial = reader.ReadUInt32BE(),
                        Cliloc = reader.ReadUInt32BE(),
                        Sentinel1 = reader.ReadUInt32BE(),
                        Entries = new List<DisplayEquipInfoEntry>()
                    };

                    var ownerNameLen = reader.ReadUInt16BE();
                    info.OwnerName = reader.ReadASCII(ownerNameLen);
                    info.Sentinel2 = reader.ReadUInt32BE();

                    while (reader.Remaining > 0)
                    {
                        var attributeId = reader.ReadUInt32BE();
                        if (attributeId == 0xFFFF_FFFF)
                            break;

                        var charges = reader.ReadInt16BE();
                        info.Entries.Add(new DisplayEquipInfoEntry
                        {
                            AttributeId = attributeId,
                            Charges = charges
                        });
                    }

                    DisplayEquipInfo = info;
                }
                break;

            case 0x14:
                {
                    ushort mode = reader.ReadUInt16BE();
                    bool isNewCliloc = mode >= 2;
                    uint serial = reader.ReadUInt32BE();
                    byte count = reader.ReadUInt8();
                    var items = new PopupMenuItemData[count];

                    for (int i = 0; i < count; i++)
                    {
                        ushort hue = 0xFFFF, replaced = 0;
                        int cliloc;
                        ushort index, flags;

                        if (isNewCliloc)
                        {
                            cliloc = (int)reader.ReadUInt32BE();
                            index = reader.ReadUInt16BE();
                            flags = reader.ReadUInt16BE();
                        }
                        else
                        {
                            index = reader.ReadUInt16BE();
                            cliloc = reader.ReadUInt16BE() + 3000000;
                            flags = reader.ReadUInt16BE();

                            if ((flags & 0x84) != 0)
                                reader.Skip(2);
                            if ((flags & 0x40) != 0)
                                reader.Skip(2);
                            if ((flags & 0x20) != 0)
                                replaced = reader.ReadUInt16BE();
                        }

                        if ((flags & 0x01) != 0)
                            hue = 0x0386;

                        items[i] = new PopupMenuItemData
                        {
                            Cliloc = cliloc,
                            Index = index,
                            Hue = hue,
                            ReplacedHue = replaced,
                            Flags = flags,
                        };
                    }

                    PopupMenu = new PopupMenuInfo { Serial = serial, Items = items };
                }
                break;

            case 0x16:
                ClosedLocalGumpType = reader.ReadUInt32BE();
                ClosedLocalGumpSerial = reader.ReadUInt32BE();
                break;

            case 0x19:
                StatsVersion = reader.ReadUInt8();
                StatsSerial = reader.ReadUInt32BE();
                break;

            case 0x1B:
                {
                    reader.Skip(sizeof(ushort)); // sub command
                    var spellInfo = new SpellbookContentData
                    {
                        Serial = reader.ReadUInt32BE(),
                        Graphic = reader.ReadUInt16BE(),
                        Type = reader.ReadUInt16BE(),
                        SpellBitfields = new uint[2]
                    };

                    for (var i = 0; i < 2; ++i)
                    {
                        uint spells = 0;
                        for (var j = 0; j < 4; ++j)
                        {
                            spells |= (uint)reader.ReadUInt8() << (j * 8);
                        }

                        spellInfo.SpellBitfields[i] = spells;
                    }

                    SpellbookContent = spellInfo;
                }
                break;

            case 0x1D:
                HouseRevisionSerial = reader.ReadUInt32BE();
                HouseRevision = reader.ReadUInt32BE();
                break;

            case 0x20:
                HouseCustomization = new HouseCustomizationData
                {
                    Serial = reader.ReadUInt32BE(),
                    Type = reader.ReadUInt8(),
                    Graphic = reader.ReadUInt16BE(),
                    X = reader.ReadUInt16BE(),
                    Y = reader.ReadUInt16BE(),
                    Z = reader.ReadInt8()
                };
                break;

            case 0x21:
                // Server cleared all queued special moves — combat book / ability
                // buttons drop the queued (0x80) tint.
                ClearAbilities = true;
                break;

            case 0x22:
                reader.Skip(1);
                DamageSerial = reader.ReadUInt32BE();
                DamageAmount = reader.ReadUInt8();
                break;

            case 0x25:
                SpellIconSpell = reader.ReadUInt16BE();
                SpellIconActive = reader.ReadBool();
                break;

            case 0x26:
                var speedMode = (CharacterSpeedType)reader.ReadUInt8();
                if (speedMode > CharacterSpeedType.FastUnmountAndCantRun)
                    speedMode = 0;
                CharacterSpeedMode = speedMode;
                break;

            case 0x2A:
                IsFemale = reader.ReadBool();
                Race = (RaceType)reader.ReadUInt8();
                break;

            case 0x2B:
                StatueAnimation = new StatueAnimationData
                {
                    Serial = reader.ReadUInt16BE(),
                    AnimationId = reader.ReadUInt8(),
                    FrameCount = reader.ReadUInt8()
                };
                break;
        }
    }
}
