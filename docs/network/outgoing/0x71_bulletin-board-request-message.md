# 0x71 — Send_BulletinBoardRequestMessage

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/BulletinBoardGump.cs:549`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | board |
| uint | msgSerial | message id |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x71 | |
| u16  | length | dynamic |
| u8   | 0x03 | subcommand: request body |
| u32  | serial | |
| u32  | msgSerial | |

## Behavior

Requests full body of a bulletin board message.
