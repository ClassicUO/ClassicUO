# 0x2D — MobileAttributes

**Direction:** in
**Length:** 17 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | HitsMax | |
| u16 | Hits | |
| u16 | ManaMax | |
| u16 | Mana | |
| u16 | StaminaMax | |
| u16 | Stamina | |

## Behavior

Writes HitsMax/Hits on the entity and (for mobiles) ManaMax/Mana/StaminaMax/Stamina; signals UoAssist hits/stamina/mana for the player.
