# 0xCC — ClilocMessageAffix

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Graphic | |
| enum:MessageType | MessageType | u8 |
| u16 | Hue | |
| u16 | Font | |
| u32 | Cliloc | |
| enum:AffixType | AffixType | u8 |
| string(30) | Name | ASCII |
| string | Affix | ASCII null-terminated |
| string | Arguments | unicode BE null-terminated |

## Behavior

Translates cliloc with optional Prepend/Append affix and System override flag, then routes through MessageManager as system/object text (disposes party invites for cliloc 1008092/1005445).
