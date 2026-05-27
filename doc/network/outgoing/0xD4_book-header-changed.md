# 0xD4 — Send_BookHeaderChanged

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/ModernBookGump.cs:310`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | book |
| string | title | |
| string | author | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD4 | |
| u16  | length | dynamic |
| u32  | serial | |
| u8   | 0x00 | |
| u8   | 0x00 | |
| u16  | 0 | |
| u16  | titleLen | UTF-8 byte count |
| utf8(titleLen) | title | |
| u16  | authorLen | UTF-8 byte count |
| utf8(authorLen) | author | |

## Behavior

Modern variable-length book title/author edit.
