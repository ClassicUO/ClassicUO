# 0x5D — Send_SelectCharacter

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Scenes/CharacterSelectionPlugin.cs:139`, `src/ClassicUO.Client/Ecs/Scenes/CharacterSelectionPlugin.cs:230`, `src/ClassicUO.Client/Agent/AgentServerPlugin.cs:227`, `src/ClassicUO.Client/Game/Scenes/LoginScene.cs:391`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | index | slot |
| string | name | |
| uint | ipclient | |
| ClientFlags | protocol | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x5D | |
| u32  | 0xEDEDEDED | pattern |
| ascii(30) | name | |
| u16  | 0 | |
| u32  | protocol | |
| 24 bytes | zero | |
| u32  | index | |
| u32  | ipclient | |

## Behavior

Picks an existing character to enter the game.
