# 0x9F — Send_SellRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/ShopGump.cs:605`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | vendor |
| Tuple<uint,ushort>[] | items | (serial, count) |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x9F | |
| u16  | length | dynamic |
| u32  | serial | |
| u16  | itemCount | |
| per item: u32 serial, u16 count | | |

## Behavior

Sells items to a vendor.
