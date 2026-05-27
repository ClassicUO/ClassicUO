# 0x7C — OpenMenu

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | MenuId | |
| string | Name | u8 length-prefixed ASCII |
| u8 | EntryCount | |
| list | Entries | per entry: u16 MenuId, u16 Hue, u8 responseLen, ASCII Response |

## Behavior

Opens MenuGump (icon menu with art tiles) when menuid != 0, else GrayMenuGump (text list with OK/Cancel buttons).
