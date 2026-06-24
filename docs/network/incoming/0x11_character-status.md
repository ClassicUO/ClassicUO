# 0x11 — CharacterStatus

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| string(30) | Name | ASCII |
| u16 | Hits | |
| u16 | HitsMax | |
| bool | CanBeRenamed | |
| u8 | Type | extension level (0..6) |
| bool? | IsFemale | Type > 0 |
| u16? | Strength | |
| u16? | Dexterity | |
| u16? | Intelligence | |
| u16? | Stamina | |
| u16? | StaminaMax | |
| u16? | Mana | |
| u16? | ManaMax | |
| u32? | Gold | |
| i16? | PhysicalResistance | |
| u16? | Weight | |
| u16? | WeightMax | Type >= 5 |
| u8? | Race | Type >= 5 |
| i16? | StatsCap | Type >= 3 |
| u8? | Followers | Type >= 3 |
| u8? | MaxFollowers | Type >= 3 |
| i16? | FireResistance | Type >= 4 |
| i16? | ColdResistance | |
| i16? | PoisonResistance | |
| i16? | EnergyResistance | |
| u16? | Luck | |
| i16? | DamageMin | |
| i16? | DamageMax | |
| u32? | TithingPoints | |
| i16? | MaxPhysical..EnergyResistance | Type >= 6 |
| i16? | DefenseChanceIncrease/Max | |
| i16? | HitChanceIncrease | |
| i16? | SwingSpeedIncrease | |
| i16? | DamageIncrease | |
| i16? | LowerReagentCost | |
| i16? | SpellDamageIncrease | |
| i16? | FasterCastRecovery | |
| i16? | FasterCasting | |
| i16? | LowerManaCost | |

## Behavior

Writes Name/Hits/HitsMax on the entity, then for mobiles writes IsRenamable/IsFemale; on the player additionally writes Str/Dex/Int/Stamina/Mana/Gold/Weight/Race plus AOS resists, ML maxes, and Renaissance follower count — printing delta messages for Str/Dex/Int changes. Signals UoAssist hits/stamina/mana for the player.
