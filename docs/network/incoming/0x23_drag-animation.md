# 0x23 — DragAnimation

**Direction:** in
**Length:** 26 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u16 | Graphic | |
| u8 | GraphicIncrement | |
| u16 | Hue | |
| u16 | Amount | |
| u32 | SourceSerial | |
| u16 | SourceX | |
| u16 | SourceY | |
| i8 | SourceZ | |
| u32 | TargetSerial | |
| u16 | TargetX | |
| u16 | TargetY | |
| i8 | TargetZ | |

## Behavior

Spawns DragEffect (or Moving when src/dst invalid) for graphic between source and dest serials/positions; pulls live coords from mobiles when present.
