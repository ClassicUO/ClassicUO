# 0x6F — Send_TradeResponse

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:704`, `src/ClassicUO.Client/Game/GameActions.cs:709`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | trade window |
| int | code | 1 = cancel, 2 = accept-state |
| bool | state | for code==2 |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x6F | |
| u16  | length | dynamic |
| u8   | code | 0x01 cancel / 0x02 accept |
| u32  | serial | |
| u32  | state | only when code==2 |

## Behavior

Cancel or accept a secure trade session.
