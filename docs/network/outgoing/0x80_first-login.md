# 0x80 — Send_FirstLogin

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Network/NetworkPlugin.cs:240`, `src/ClassicUO.Client/Ecs/Modding/ModdingPlugin.cs:269`, `src/ClassicUO.Client/Game/Scenes/LoginScene.cs:515`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | user | |
| string | psw | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x80 | |
| ascii(30) | user | zero-padded |
| ascii(30) | psw | zero-padded |
| u8   | 0xFF | next-login-key |

## Behavior

Account login to login server.
