# 0xD1 — LogoutRequest

**Direction:** in
**Length:** 2 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| bool | ShouldDisconnect | |

## Behavior

When a disconnect was requested and CLF_OWERWRITE_CONFIGURATION_BUTTON is set, server's accept byte triggers NetClient.Disconnect() and switch to LoginScene.
