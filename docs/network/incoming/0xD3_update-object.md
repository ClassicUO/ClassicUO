# 0xD3 — UpdateObject

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

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
| u16[3] | Reserved | three u16 padding |
| list | Equipment | until serial==0; per item: u32 Serial, u16 Graphic (high bit selects hue mode), u8 Layer, u16 Hue when remaining >= 2 |

## Behavior

Same as 0x78 with trailing padding; spawns/updates the mobile at x/y/z/direction with hue/flags/NotorietyFlag, rebuilds equipment list, swaps season on death-state change, refreshes PaperDollGump + UpdateAbilities for the player.
