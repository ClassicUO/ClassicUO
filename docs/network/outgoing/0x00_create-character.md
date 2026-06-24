# 0x00 — Send_CreateCharacter

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/Scenes/LoginScene.cs:417`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| PlayerMobile | character | |
| int | cityIndex | |
| uint | clientIP | |
| int | serverIndex | |
| uint | slot | |
| byte | profession | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID | 0x00 (<7.0.16.0) or 0xF8 (>=7.0.16.0) |
| u32  | 0xEDEDEDED | pattern |
| u32  | 0xFFFFFFFF | |
| u8   | 0x00 | |
| ascii(30) | name | |
| u16  | 0 | |
| u32  | protocol | client flags |
| u32  | 0x01 | |
| u32  | 0x00 | |
| u8   | profession | |
| 15 bytes | zero | |
| u8   | race+gender | encoded (race*2 + female-bit) |
| u8   | str | |
| u8   | dex | |
| u8   | int | |
| u8,u8 x 3-4 | skills | (index, value) per skill; 4 skills on >=7.0.16.0 |
| u16  | hue | skin |
| u16,u16 | hair graphic, hue | |
| u16,u16 | beard graphic, hue | |
| u16  | cityIndex | |
| u16  | 0 | |
| u16  | slot | |
| u32  | clientIP | |
| u16  | shirt hue | |
| u16  | pants hue | |

## Behavior

Creates a new character on the chosen shard slot.
