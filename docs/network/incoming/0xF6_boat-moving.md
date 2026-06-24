# 0xF6 — BoatMoving

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u8 | Speed | |
| enum:Direction | MovingDirection | u8 |
| enum:Direction | FacingDirection | u8 |
| u16 | X | |
| u16 | Y | |
| u16 | Z | |
| u16 | PassengerCount | |
| list | Passengers | PassengerCount entries of (u32 Serial, u16 X, u16 Y, u16 Z) |

## Behavior

Smooth mode: queues a BoatMovingManager step (speed/movingDir/facingDir/x/y/z) for the multi plus per-passenger offsets. Non-smooth: snaps multi to tile, regenerates house components, and re-positions each passenger entity.
