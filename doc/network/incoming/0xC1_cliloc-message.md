# 0xC1 — ClilocMessage

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
| string(30) | Name | ASCII |
| string | Arguments | unicode LE, fills remaining bytes |

## Behavior

Translates cliloc with unicode LE arguments and routes through MessageManager as object/system text using the entity (sets entity.Name when blank).
