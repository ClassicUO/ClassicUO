# 0xBF — Send_ToggleGargoyleFlying

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/MacroManager.cs:1457`, `src/ClassicUO.Client/Game/UI/Gumps/RacialAbilityButton.cs:46`, `src/ClassicUO.Client/Game/UI/Gumps/RacialAbilitiesBookGump.cs:195`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x32 | subcommand: gargoyle fly |
| u16  | 0x01 | |
| u32  | 0 | |

## Behavior

Toggles gargoyle flight mode.
