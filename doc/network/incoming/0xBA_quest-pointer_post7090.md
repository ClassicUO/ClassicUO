# 0xBA — QuestPointer

**Direction:** in
**Length:** 10 bytes
**Variant:** Post7090 — used when ClientVersion >= 7.0.9.0

## Fields

| Type | Name | Notes |
|------|------|-------|
| bool | Display | |
| u16 | X | |
| u16 | Y | |
| u32 | Serial | |

## Behavior

Display true: opens or repositions QuestArrowGump for serial at mx/my; display false: disposes it.
