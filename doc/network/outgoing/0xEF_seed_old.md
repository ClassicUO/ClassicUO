# 0xEF — Send_Seed_Old

**Direction:** out
**Length:** fixed 4 bytes
**Callers:** `src/ClassicUO.Client/Ecs/Network/NetworkPlugin.cs:237`, `src/ClassicUO.Client/Ecs/Modding/ModdingPlugin.cs:266`, `src/ClassicUO.Client/Game/Scenes/LoginScene.cs:512`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | v | seed value |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u32  | v | bare seed, no packet id byte |

## Behavior

Legacy pre-6.0.5.0 seed handshake — 4 bare bytes, no ID prefix.
