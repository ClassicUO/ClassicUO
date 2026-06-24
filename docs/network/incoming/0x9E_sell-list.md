# 0x9E — SellList

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Count | |
| list | Entries | per entry: u32 ItemSerial, u16 Graphic, u16 Hue, u16 Amount, u16 Price, u16 nameLen, ASCII Name |

## Behavior

Disposes any existing ShopGump for vendor and opens a sell ShopGump populated with each item (serial/graphic/hue/amount/price/name with cliloc and OPL fallbacks).
