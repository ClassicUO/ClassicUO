# 0x1A — UpdateItem

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | high bit signals HasAmount, masked off |
| u16 | Graphic | high bit signals graphic increment present |
| u8? | GraphicIncrement | when Graphic high bit set |
| u16? | Amount | when Serial high bit set, else 1 |
| u16 | X | high bit signals HasDirection |
| u16 | Y | bit 0x8000 = HasHue, bit 0x4000 = HasFlags |
| enum:Direction | Direction | u8, only when HasDirection |
| i8 | Z | |
| u16? | Hue | when HasHue |
| enum:Flags | Flags | u8, when HasFlags |
| u8 | Type | derived: 2 if Graphic >= 0x4000 (multi), else 0 |

## Behavior

Spawns/updates item game object (graphic/graphicInc/amount/x/y/z/direction/hue/flags); multi items (graphic >= 0x4000) use type 2. Generates floating text/object effects via UpdateGameObject helper.
