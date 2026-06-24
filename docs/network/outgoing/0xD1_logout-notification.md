# 0xD1 — Send_LogoutNotification

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs:277`, `src/ClassicUO.Client/Game/Scenes/GameScene.cs:390`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD1 | |
| u8   | 0x00 | |

## Behavior

Asks server for safe-logout permission.
