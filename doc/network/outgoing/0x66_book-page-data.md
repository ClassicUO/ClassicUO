# 0x66 — Send_BookPageData

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/ModernBookGump.cs:326`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | book |
| string[] | text | one entry per line |
| int | page | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x66 | |
| u16  | length | dynamic |
| u32  | serial | |
| u16  | 0x01 | page count |
| u16  | page | page index |
| u16  | lineCount | |
| utf8 + 0x00 per line | text | newlines stripped |
| u8   | 0x00 | trailing terminator |

## Behavior

Writes page contents back to the server.
