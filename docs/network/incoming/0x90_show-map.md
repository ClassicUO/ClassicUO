# 0x90 — ShowMap

**Direction:** in
**Length:** 19 or 21 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | GumpId | |
| u16 | StartX | |
| u16 | StartY | |
| u16 | EndX | |
| u16 | EndY | |
| u16 | Width | |
| u16 | Height | |
| u16? | Facet | present when remaining >= 2 |

## Behavior

Opens MapGump for serial with gumpid/width/height; binds the MultiMap texture (facet 0 default, or read from 0xF5) and marks the source item Opened.
