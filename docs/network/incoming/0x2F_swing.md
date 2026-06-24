# 0x2F — Swing

**Direction:** in
**Length:** 10 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | (skipped) | |
| u32 | AttackerSerial | |
| u32 | DefenderSerial | |

## Behavior

When attacker is player in war mode and defender == LastAttack and player idle, auto-turn player to face the enemy direction.
