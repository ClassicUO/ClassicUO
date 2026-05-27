# 0xB3 — Send_ChatCreateChannelCommand

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/ChatGump.cs:416`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | name | channel name |
| string | password | optional |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xB3 | |
| u16  | length | dynamic |
| ascii(4) | lang | |
| u16  | 0x63 | command: create channel |
| unicodeBE | name | |
| u16  | 0x7B | optional `{` |
| unicodeBE | password | optional |
| u16  | 0x07D | optional `}` |

## Behavior

Creates a new chat channel.
