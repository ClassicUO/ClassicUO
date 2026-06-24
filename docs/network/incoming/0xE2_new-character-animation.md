# 0xE2 — NewCharacterAnimation

**Direction:** in
**Length:** 10 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Type | |
| u16 | Action | |
| u8 | Mode | |

## Behavior

Plays the new-animation group derived from type/action/mode on the mobile (repeat on type 1/2 when graphic == 0x0015).
