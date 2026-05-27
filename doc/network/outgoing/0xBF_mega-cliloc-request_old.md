# 0xBF — Send_MegaClilocRequest_Old

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Network/PacketHandlers.cs:328`, `src/ClassicUO.Client/Network/PacketHandlers.cs:4338`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x10 | subcommand: cliloc request (legacy) |
| u32  | serial | |

## Behavior

Legacy single-serial mega-cliloc tooltip request.
