# 0x27 — DenyMoveItem

**Direction:** in
**Length:** 2 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Code | reason |

## Behavior

Restores the dragged ItemHold back into its prior container/paperdoll/terrain, clears the cursor hold, and surfaces a cliloc error message for code < 5.
