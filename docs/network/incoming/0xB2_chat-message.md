# 0xB2 — ChatMessage

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u16 | Command | |

## Behavior

Dispatches by command: create/destroy/join/leave conference channels, enable/disable chat, username request/accept (sends ChatJoinCommand "General"), add/remove user, and prints chat lines or localized Chat.enu strings via MessageManager.
