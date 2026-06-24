# 0x20 — UpdatePlayer

**Direction:** in
**Length:** 19 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Graphic | |
| u8 | GraphicIncrement | |
| u16 | Hue | |
| enum:Flags | Flags | u8 |
| u16 | X | |
| u16 | Y | |
| u16 | ServerId | |
| enum:Direction | Direction | u8 |
| i8 | Z | |

## Behavior

Updates the player entity (graphic/graphic_inc/hue/flags/x/y/z/direction) via UpdatePlayer helper.
