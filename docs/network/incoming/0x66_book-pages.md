# 0x66 — BookPages

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | PageCount | |
| list | Pages | each: u16 Number, u16 linesCount, linesCount ASCII strings |

## Behavior

Populates pages (lines per page) into the open ModernBookGump for serial; marks pages known, refreshes text.
