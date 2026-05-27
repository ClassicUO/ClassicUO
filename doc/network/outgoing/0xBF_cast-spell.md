# 0xBF — Send_CastSpell

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:647`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| int | idx | spell id |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID | 0xBF (>=6.0.1.42) / 0x12 (older) |
| u16  | length | dynamic |
| u16  | 0x1C | subcommand (new path) |
| u16  | 0x02 | |
| u16  | idx | spell id |

For old client path (0x12): `u8 0x56` + ascii(idx).

## Behavior

Cast spell index — uses generic-command (0xBF) on modern clients, action-cmd (0x12) on legacy.
