# 0x12 — Send_UseSkill

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:671`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| int | idx | skill index |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x12 | |
| u16  | length | dynamic |
| u8   | 0x24 | action subcommand |
| ascii | "{idx} 0" | null-terminated |

## Behavior

Triggers the use of a skill.
