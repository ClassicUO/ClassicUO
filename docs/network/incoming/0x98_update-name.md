# 0x98 — UpdateName

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| string | Name | ASCII null-terminated |

## Behavior

Sets Name on the entity (and WMap entity); refreshes any NameOverheadGump; updates window title when it's the player.
