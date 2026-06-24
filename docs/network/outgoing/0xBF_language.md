# 0xBF — Send_Language

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:429`, `src/ClassicUO.Client/Network/PacketHandlers.cs:929`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | lang | 3-char locale tag |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x0B | subcommand: language |
| ascii(3) | lang | |
| u8   | 0x00 | terminator |

## Behavior

Announces client UI language to server.
