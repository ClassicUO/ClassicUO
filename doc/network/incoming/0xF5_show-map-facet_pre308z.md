# 0xF5 — ShowMapFacet

**Direction:** in
**Length:** 19 bytes
**Variant:** Pre308Z — used when ClientVersion < 3.0.8.z

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

## Behavior

Opens MapGump for serial with gumpid/width/height and binds the MultiMap texture (no facet selection); marks the source item Opened.
