# 0xBF — ExtendedCommand

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u16 | Command | sub-command selector |
| varies | (per Command) | 0x01 FastWalkKeys (6 * u32); 0x02 FastWalkNewKey u32; 0x04 ClosedGump (u32, i32); 0x08 MapIndex u8; 0x0C StatusBarSerial u32; 0x10 DisplayEquipInfo block; 0x16 ClosedLocalGump (u32, u32); 0x19 Stats (u8 ver, u32 serial); 0x1B SpellbookContent block; 0x1D HouseRevision (u32, u32); 0x20 HouseCustomization block; 0x22 Damage (skip 1, u32, u8); 0x25 SpellIcon (u16 spell, bool active); 0x26 CharacterSpeedMode u8; 0x2A IsFemale/Race; 0x2B StatueAnimation (u16,u8,u8) |

## Behavior

Dispatches on sub-command: 0x01/0x02 fast-walk stack, 0x04 close server gump, 0x06 party packet, 0x08 swap map index, 0x0C close HealthBarGump, 0x10 display equip info (translates clilocs, sends Send_MegaClilocRequest_Old), 0x14 popup menu, 0x16 close paperdoll/statusbar/profile/container, 0x18 apply map patches, 0x19 extended stats (lock/dead/animation), 0x1B spellbook content, 0x1D house revision (enqueues CustomHouseDataRequest), 0x20 house customization gump, 0x21 clear abilities, 0x22 damage text, 0x25 spell icon active hue, 0x26 speed mode, 0x2A race change gump, 0x2B statue animation.
