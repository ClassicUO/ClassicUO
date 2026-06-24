# 0xBA — QuestPointer

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| bool | Display | |
| u16 | X | |
| u16 | Y | |
| u32? | Serial | present if remaining >= 4 |

## Behavior

Display flag true: opens or repositions QuestArrowGump for serial at relative mx/my. Display false: disposes the QuestArrowGump.
