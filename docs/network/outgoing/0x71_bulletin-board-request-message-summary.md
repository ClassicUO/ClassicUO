# 0x71 — Send_BulletinBoardRequestMessageSummary

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Network/PacketHandlers.cs:6110`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | board |
| uint | msgSerial | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x71 | |
| u16  | length | dynamic |
| u8   | 0x04 | subcommand: request summary |
| u32  | serial | |
| u32  | msgSerial | |

## Behavior

Requests subject/author summary of a bulletin board message.
