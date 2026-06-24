# 0x77 — UpdateCharacter

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

Updates mobile NotorietyFlag; for player only writes Flags/Graphic/Hue (ignores x/y/z to avoid elastic snapback); for others spawns/updates game object at new tile/direction/hue/flags.
