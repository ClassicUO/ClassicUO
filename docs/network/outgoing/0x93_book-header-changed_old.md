# 0x93 — Send_BookHeaderChanged_Old

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/ModernBookGump.cs:314`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | book |
| string | title | |
| string | author | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x93 | |
| u32  | serial | |
| u8   | 0x00 | |
| u8   | 0x01 | |
| u16  | 0 | |
| utf8(60) | title | |
| utf8(30) | author | |

## Behavior

Legacy book title/author edit (fixed-width fields).
