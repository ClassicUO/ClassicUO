# 0x74 — BuyList

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | ContainerSerial | |
| u8 | Count | |
| list | Entries | per entry: u32 Price, u8 nameLen, ASCII(nameLen) Name |

## Behavior

Spawns/refreshes ShopGump for the container's vendor, then walks the container's items writing Price/Name (OPL/cliloc/tiledata fallback) for each.
