# 0xB3 — Send_ChatLeaveChannelCommand

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/ChatGump.cs:228`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xB3 | |
| u16  | length | dynamic |
| ascii(4) | lang | |
| u16  | 0x43 | command: leave channel |

## Behavior

Leaves current chat channel.
