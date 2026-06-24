# 0x24 — OpenContainer

**Direction:** in
**Length:** 7 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Graphic | container gump id |

## Behavior

Opens spellbook gump for Graphic 0xFFFF (plays sound 0x55), shop gump for 0x30 populating vendor's buy layers, otherwise container gump (with large-gump remap >= 706000); clears prior contents, plays open sound, marks item Opened.
