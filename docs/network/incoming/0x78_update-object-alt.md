# 0x78 — UpdateObjectAlt

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
| list | Equipment | until serial==0; per item: u32 Serial, u16 Graphic (high bit selects hue read mode), u8 Layer, u16 Hue when remaining >= 2 |

## Behavior

Spawns/updates the mobile at x/y/z/direction with hue/flags and NotorietyFlag; rebuilds equipment list (removes prior unopened non-backpack items, re-attaches each layer entry); on death-state change swaps season (Desolation on death, restore on revive) and refreshes PaperDollGump + UpdateAbilities for the player.
