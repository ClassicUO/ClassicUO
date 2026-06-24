# 0xBF — Send_PopupMenuSelection

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:689`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |
| ushort | menuid | chosen entry |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x15 | subcommand: popup menu select |
| u32  | serial | |
| u16  | menuid | |

## Behavior

Selects an entry from a context menu.
