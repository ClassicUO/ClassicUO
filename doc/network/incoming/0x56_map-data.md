# 0x56 — MapData

**Direction:** in
**Length:** 11 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| enum:MapMessageType | MessageType | u8 |
| bool | PlotEnabled | |
| u16 | X | |
| u16 | Y | |

## Behavior

Mutates the open MapGump for serial: Add pin at x/y, Clear pins, or EditResponse plot state.
