# 0x72 — Send_ChangeWarMode

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs:313`, `src/ClassicUO.Client/Game/GameActions.cs:44`, `src/ClassicUO.Client/Game/Scenes/GameSceneInputHandler.cs:1167`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| bool | state | war/peace |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x72 | |
| bool | state | |
| u8   | 0x32 | |
| u8   | 0x00 | |

## Behavior

Toggles war/peace mode.
