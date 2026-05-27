# 0xEF — Send_Seed

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Network/NetworkPlugin.cs:233`, `src/ClassicUO.Client/Ecs/Modding/ModdingPlugin.cs:262`, `src/ClassicUO.Client/Game/Scenes/LoginScene.cs:508`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | v | seed value |
| byte | major | client major version |
| byte | minor | |
| byte | build | |
| byte | extra | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xEF | |
| u32  | v | seed |
| u32  | major | written as u32BE |
| u32  | minor | |
| u32  | build | |
| u32  | extra | |

## Behavior

Initial login seed + client version handshake (post-6.0.5.0).
