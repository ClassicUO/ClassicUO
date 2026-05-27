# 0xBD — Send_ClientVersion

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:432`, `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:486`, `src/ClassicUO.Client/Network/PacketHandlers.cs:932`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | version | dotted version string |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBD | |
| u16  | length | dynamic |
| ascii | version | null-terminated |

## Behavior

Reports client version to server in response to 0xBD query.
