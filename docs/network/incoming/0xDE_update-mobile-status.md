# 0xDE — UpdateMobileStatus

**Direction:** in
**Length:** 6 or 10 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u8 | Status | |
| u32? | OpponentSerial | only when Status == 1 |

## Behavior

Reads status byte (and attacker serial when status == 1); no state mutation.
