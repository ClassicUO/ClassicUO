# 0xA1 — UpdateHits

**Direction:** in
**Length:** 9 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | HitsMax | |
| u16 | Hits | |

## Behavior

Writes HitsMax/Hits on the entity; clears pending HitsRequest; signals UoAssist hits for the player.
