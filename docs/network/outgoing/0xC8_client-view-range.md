# 0xC8 — Send_ClientViewRange

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:479`, `src/ClassicUO.Client/Network/PacketHandlers.cs:2256`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| byte | range | clamped to MIN/MAX_VIEW_RANGE |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xC8 | |
| u8   | range | |

## Behavior

Sets client's view range so server scopes object updates accordingly.
