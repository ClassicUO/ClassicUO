# 0xBA — QuestPointer

**Direction:** in
**Length:** 6 bytes
**Variant:** Pre7090 — used when ClientVersion < 7.0.9.0

## Fields

| Type | Name | Notes |
|------|------|-------|
| bool | Display | |
| u16 | X | |
| u16 | Y | |

## Behavior

Display true: opens or repositions QuestArrowGump (serial 0) at mx/my; display false: disposes it.
