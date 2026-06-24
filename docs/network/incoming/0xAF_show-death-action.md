# 0xAF — ShowDeathAction

**Direction:** in
**Length:** 13 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u32 | CorpseSerial | |
| u32 | Action | |

## Behavior

Reseats the dead mobile under OR'd-0x80000000 serial, registers its corpse, plays the death animation (walking variant when running flag set); auto-opens corpses if profile enabled.
