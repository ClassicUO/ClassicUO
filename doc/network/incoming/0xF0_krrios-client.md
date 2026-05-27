# 0xF0 — KrriosClient

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | PacketType | 1/2 = party locations, otherwise opaque |
| bool | LocationsOnly | type 1: implicit true; type 2: read |
| list | Locations | until serial==0; each entry: u32 Serial, u16 X, u16 Y, u8 Map, u8 Hits (type 2 only) |
| bytes | ExtraData | for unknown PacketType, captures remaining bytes |

## Behavior

Sub 0: enables WMapManager (ACK accepted). Sub 1/2: updates party/guild member positions on the world map. Sub 0xFE: schedules Send_RazorACK after 5s.
