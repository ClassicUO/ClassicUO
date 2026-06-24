# 0x2C — ShowDeathScreen

**Direction:** in
**Length:** 2 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Action | |

## Behavior

Resets weather, starts death music, arms DeathScreenTimer, sends RequestWarMode(off) — skipped when action == 1.
