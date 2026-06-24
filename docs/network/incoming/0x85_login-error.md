# 0x85 — LoginError

**Direction:** in
**Length:** 2 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Code | error code |

## Behavior

Routes to LoginScene.HandleErrorCode which surfaces the localized login rejection in the login UI; ignored once in-game.
