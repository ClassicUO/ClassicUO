# 0xAA — AttackEntity

**Direction:** in
**Length:** 5 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |

## Behavior

Sends CloseStatus for prior LastAttack, sets TargetManager.LastAttack = serial, sends RequestMobileStatus for new target.
