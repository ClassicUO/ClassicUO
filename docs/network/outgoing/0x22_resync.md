# 0x22 — Send_Resync

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/Movement/PlayerMovementPlugin.cs:392`, `src/ClassicUO.Client/Game/Managers/WalkerManager.cs:156`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x22 | |

## Behavior

Resync request after walk desync — server replays position state.
