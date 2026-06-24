# 0xBF — Send_StatLockStateRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:658`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| byte | stat | str/dex/int |
| Lock | state | up/down/locked |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x1A | subcommand: stat lock |
| u8   | stat | |
| u8   | state | |

## Behavior

Sets the up/down/locked indicator on a primary stat.
