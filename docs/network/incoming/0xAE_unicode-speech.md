# 0xAE — UnicodeSpeech

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
| string(4) | Language | ASCII |
| string(30) | Name | ASCII |
| unicode | Text | BE null-terminated (absent if system message) |

## Behavior

For "system" zero-hue/font handshake: sends a hard-coded 40-byte ACK. Otherwise tags text as GUILD_ALLY/OBJECT/SYSTEM and routes through MessageManager.HandleMessage with language (writes entity.Name when blank).
