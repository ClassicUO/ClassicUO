# 0x91 — Send_SecondLogin

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Network/LoginPacketsPlugin.cs:139`, `src/ClassicUO.Client/Game/Scenes/LoginScene.cs:687`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | user | |
| string | psw | |
| uint | seed | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x91 | |
| u32  | seed | |
| ascii(30) | user | |
| ascii(30) | psw | |

## Behavior

Auth post-relay to game server.
