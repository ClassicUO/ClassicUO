# 0x6F — Send_TradeUpdateGold

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/TradingGump.cs:517`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | trade window |
| uint | gold | |
| uint | platinum | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x6F | |
| u16  | length | dynamic |
| u8   | 0x03 | subcommand: update gold |
| u32  | serial | |
| u32  | gold | |
| u32  | platinum | |

## Behavior

Updates secure-trade currency offers.
