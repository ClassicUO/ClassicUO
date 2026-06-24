# 0xF5 — ShowMapFacet

**Direction:** in
**Length:** 21 bytes
**Variant:** Post308Z — used when ClientVersion >= 3.0.8.z

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
| u16 | Facet | |

## Behavior

Opens MapGump for serial with gumpid/width/height and binds the MultiMap texture for the given Facet; marks the source item Opened.
