# 0xBF — Send_CustomHouseDataRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Network/PacketHandlers.cs:339`, `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:624`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | foundation |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x1E | subcommand: custom house data |
| u32  | serial | |

## Behavior

Requests the customizable house design payload.
