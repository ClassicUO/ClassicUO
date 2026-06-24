# 0x22 — ConfirmWalk

**Direction:** in
**Length:** 3 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Sequence | |
| enum:NotorietyFlag | Notoriety | u8 |

## Behavior

Confirms walk seq, sets player NotorietyFlag, commits step to tile.
