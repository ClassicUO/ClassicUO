# 0xBF — Send_RequestPopupMenu

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:684`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x13 | subcommand: popup menu request |
| u32  | serial | |

## Behavior

Requests a context-menu listing for an entity.
