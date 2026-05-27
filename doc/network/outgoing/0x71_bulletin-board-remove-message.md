# 0x71 — Send_BulletinBoardRemoveMessage

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/BulletinBoardGump.cs:449`

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
| u8   | 0x06 | subcommand: remove |
| u32  | serial | |
| u32  | msgSerial | |

## Behavior

Deletes a bulletin board message.
