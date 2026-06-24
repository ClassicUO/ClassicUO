# 0xBF — Send_ClickQuestArrow

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:816`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| bool | rightClick | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x07 | subcommand: quest arrow click |
| bool | rightClick | |

## Behavior

Reports click on the active quest arrow.
