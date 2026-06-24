# 0x71 — Send_BulletinBoardPostMessage

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/BulletinBoardGump.cs:422`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | board |
| uint | msgSerial | parent (0 for new) |
| string | subject | |
| string | text | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x71 | |
| u16  | length | dynamic |
| u8   | 0x05 | subcommand: post |
| u32  | serial | |
| u32  | msgSerial | |
| u8   | subjectLen+1 | |
| utf8 | subject | null-terminated |
| u8   | lineCount | |
| (u8 lineLen+1, utf8 line, 0x00) per line | CRLF normalized to LF then split |

## Behavior

Posts a new (or reply) bulletin board message.
