# 0xDD — OpenCompressedGump

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Sender | |
| u32 | GumpId | |
| i32 | X | |
| i32 | Y | |
| u32 | LayoutCompressedLength | stored value minus 4 |
| u32 | LayoutDecompressedLength | |
| bytes | LayoutData | LayoutCompressedLength bytes |
| u32 | LinesCount | |
| u32 | LinesCompressedLength | when LinesCount > 0, stored minus 4 |
| u32 | LinesDecompressedLength | |
| bytes | LinesData | LinesCompressedLength bytes |

## Behavior

ZLib-decompresses the layout and text-lines blocks, then builds a generic server gump (CreateGump) at x/y for sender/gumpID.
