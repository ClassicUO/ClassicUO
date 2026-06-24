# 0xD8 — CustomHouse

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | IsCompressed | == 0x03 |
| bool | Response | |
| u32 | Serial | |
| u32 | Revision | |
| u32 | (skipped) | |
| u8 | planesCount | |
| list | Planes | per plane: u32 header (encodes lengths + planeZ + planeMode), bytes compressed (ZLib decompressed into PlaneData.Data) |

## Behavior

Builds or refreshes the House for the foundation: ZLib-decompresses each plane and adds tiles per planeMode (0 = id+xyz, 1 = id+xy with z derived, 2 = id only with x/y derived from planeZ offsets). Triggers GenerateFloorPlace if HouseCustomizationGump is open, MiniMap refresh, MaxDrawZ refresh when player is inside, and clears boat steps.
