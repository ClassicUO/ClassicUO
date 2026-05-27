# 0xD6 — Send_MegaClilocRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Network/PacketHandlers.cs:321`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| List<uint> | serials | mutated — drained in batches of 15 |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD6 | |
| u16  | length | dynamic |
| u32[] | serials | up to 15 per packet |

## Behavior

Batched mega-cliloc tooltip request. Drains up to 15 serials per call.
