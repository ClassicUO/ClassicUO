# 0xD2 — UpdateCharacterAlt

**Direction:** in
**Length:** 17 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Graphic | |
| u16 | X | |
| u16 | Y | |
| i8 | Z | |
| enum:Direction | Direction | u8 |
| u16 | Hue | |
| enum:Flags | Flags | u8 |
| enum:NotorietyFlag | Notoriety | u8 |

## Behavior

Same as 0x77; updates mobile NotorietyFlag; for player writes Flags/Graphic/Hue only, otherwise spawns/updates game object at new tile.
