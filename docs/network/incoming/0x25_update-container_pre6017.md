# 0x25 — UpdateContainer

**Direction:** in
**Length:** 20 bytes
**Variant:** Pre6017 — used when ClientVersion < 6.0.1.7 (no GridIndex)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Graphic | |
| i8 | GraphicIncrement | |
| u16 | Amount | 0 normalized to 1 |
| u16 | X | |
| u16 | Y | |
| u32 | ContainerSerial | |
| u16 | Hue | |

## Behavior

Clears prior cursor hold when its serial matches, then adds the item to the container with graphic/amount/hue/x/y; refreshes the open ContainerGump.
