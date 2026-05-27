# 0xB0 — OpenGump

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Sender | |
| u32 | GumpId | |
| i32 | X | |
| i32 | Y | |
| u16 | CommandLength | |
| string | Command | ASCII(CommandLength) |
| u16 | LinesCount | |
| list | Lines | per line: u16 length, unicode BE Text(length) |

## Behavior

Parses layout cmd + unicode text lines and builds a generic server gump (CreateGump) at x/y for sender/gumpID.
