# 0xE5 — AddWaypoint

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | X | |
| u16 | Y | |
| i8 | Z | |
| u8 | Map | |
| enum:WaypointsType | WaypointType | u16 BE |
| bool | IgnoreObject | u16 BE != 0 |
| u32 | Cliloc | |
| unicode | Name | LE, null-terminated |

## Behavior

Reads waypoint fields (serial/x/y/z/map/type/ignore/cliloc/name); no state mutation.
