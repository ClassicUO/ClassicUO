# 0xB3 — Send_ChatMessageCommand

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/SystemChatControl.cs:847`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | msg | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xB3 | |
| u16  | length | dynamic |
| ascii(4) | lang | |
| u16  | 0x61 | command: say |
| unicodeBE | msg | |

## Behavior

Sends a message to current chat channel.
