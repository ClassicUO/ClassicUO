# 0x12 — Send_OpenSpellBook

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/MacroManager.cs:575`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| byte | type | spellbook discipline |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x12 | |
| u16  | length | dynamic |
| u8   | 0x43 | subcommand |
| u8   | type | |

## Behavior

Opens player's spellbook of the given discipline.
