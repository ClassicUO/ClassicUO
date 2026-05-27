# 0x99 — PlaceMulti

**Direction:** in
**Length:** 32 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| bool | OnGround | |
| u32 | TargetSerial | |
| u8 | Flags | |
| bytes(18) | UnknownData | |
| u16 | MultiId | |
| i16 | OffsetX | |
| i16 | OffsetY | |
| i16 | OffsetZ | |
| u16 | Hue | |

## Behavior

Arms TargetManager to place a multi (targID/multiID/xOff/yOff/zOff/hue).
