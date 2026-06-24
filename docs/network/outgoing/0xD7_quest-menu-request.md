# 0xD7 — Send_QuestMenuRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs:286`, `src/ClassicUO.Client/Game/GameActions.cs:567`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | playerSerial | overload also accepts World |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD7 | |
| u16  | length | dynamic |
| u32  | playerSerial | |
| u16  | 0x32 | subcommand: quest menu |
| u8   | 0x00 | |

## Behavior

Opens the quest log gump.
