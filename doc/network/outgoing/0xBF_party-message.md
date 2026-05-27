# 0xBF — Send_PartyMessage

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:404`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | text | |
| uint | serial | target member or invalid for all |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x06 | subcommand: party |
| u8   | 0x03/0x04 | private-to (with serial u32) / broadcast |
| u32  | serial | only when private |
| unicodeBE | text | |

## Behavior

Sends party chat (private if serial valid, broadcast otherwise).
