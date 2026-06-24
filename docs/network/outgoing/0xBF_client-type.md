# 0xBF — Send_ClientType

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:476`, `src/ClassicUO.Client/Network/PacketHandlers.cs:2251`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| ClientFlags | protocol | expansion bitmask |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x0F | subcommand: client type |
| u8   | 0x0A | |
| u32  | clientFlag | expansion bits computed from protocol |

## Behavior

Reports client expansion / feature flags to server.
