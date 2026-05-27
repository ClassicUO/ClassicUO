# 0xA0 — Send_SelectServer

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Scenes/ServerSelectionPlugin.cs:163`, `src/ClassicUO.Client/Ecs/Modding/ModdingPlugin.cs:276`, `src/ClassicUO.Client/Game/Scenes/LoginScene.cs:380`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| byte | index | shard index |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xA0 | |
| u8   | 0x00 | reserved |
| u8   | index | |

## Behavior

Picks a game shard from the login server list.
