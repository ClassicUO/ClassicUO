# 0x1C — AsciiSpeech

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
| string(30) | Name | ASCII fixed |
| string | Text | ASCII null-terminated (absent if system message) |

## Behavior

For "SYSTEM" zero-hue/font handshake: sends Send_ACKTalk. Otherwise tags text as OBJECT/SYSTEM based on serial/name/entity and routes through MessageManager.HandleMessage (writing entity.Name when blank).
