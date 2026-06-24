# 0x21 — DenyWalk

**Direction:** in
**Length:** 8 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Sequence | |
| u16 | X | |
| u16 | Y | |
| enum:Direction | Direction | u8 |
| i8 | Z | |

## Behavior

Rejects walk seq, snaps player back to x/y/z/direction, resets weather.
