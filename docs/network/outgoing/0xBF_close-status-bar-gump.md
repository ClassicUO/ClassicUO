# 0xBF — Send_CloseStatusBarGump

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:628`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | mobile |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x0C | subcommand: close status bar |
| u32  | serial | |

## Behavior

Tells server the status bar for serial was closed.
