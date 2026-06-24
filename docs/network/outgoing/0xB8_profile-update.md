# 0xB8 — Send_ProfileUpdate

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/ProfileGump.cs:219`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |
| string | text | new profile text |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xB8 | |
| u16  | length | dynamic |
| u8   | 0x01 | subcommand: write |
| u32  | serial | |
| u16  | 0x01 | |
| u16  | textLen | |
| unicode(textLen) | text | |

## Behavior

Writes new character profile text.
