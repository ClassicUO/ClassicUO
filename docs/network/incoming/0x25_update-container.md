# 0x25 — UpdateContainer

**Direction:** in
**Length:** 20 or 21 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Graphic | |
| i8 | GraphicIncrement | |
| u16 | Amount | 0 normalized to 1 |
| u16 | X | |
| u16 | Y | |
| u8? | GridIndex | present if remaining >= u8+u32+u16 |
| u32 | ContainerSerial | |
| u16 | Hue | |

## Behavior

Clears prior cursor hold when its serial matches, then adds the item to the container (RemoveItemFromContainer, push under containerSerial) with graphic/amount/hue/x/y; refreshes the open ContainerGump.
