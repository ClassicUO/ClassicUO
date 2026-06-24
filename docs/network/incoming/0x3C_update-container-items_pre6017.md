# 0x3C — UpdateContainerItems

**Direction:** in
**Length:** dynamic (length-prefixed ushort)
**Variant:** Pre6017 — used when ClientVersion < 6.0.1.7 (no GridIndex field)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u16 | Count | |
| list | Items | per entry: u32 Serial, u16 Graphic, u8 GraphicInc, u16 Amount, u16 X, u16 Y, u32 ContainerSerial, u16 Hue (GridIndex implicit 0) |

## Behavior

For each entry: on first iteration clears the container's prior contents (corpse-aware), then adds the item under its containerSerial with graphic/amount/hue/x/y.
