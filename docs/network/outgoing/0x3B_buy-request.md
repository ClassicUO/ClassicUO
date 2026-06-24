# 0x3B — Send_BuyRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/ShopGump.cs:601`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | vendor |
| Tuple<uint,ushort>[] | items | (serial, count) |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x3B | |
| u16  | length | dynamic |
| u32  | serial | |
| u8   | 0x02 or 0x00 | flag if items present |
| per item: u8 0x1A, u32 serial, u16 count | | |

## Behavior

Buys items from a vendor.
