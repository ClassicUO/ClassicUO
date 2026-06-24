# 0x12 — Send_CastSpellFromBook

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:638`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| int | idx | spell index |
| uint | serial | spellbook serial |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x12 | |
| u16  | length | dynamic |
| u8   | 0x27 | action subcommand |
| ascii | "{idx} {serial}" | null-terminated |

## Behavior

Cast a spell from a specific spellbook serial.
