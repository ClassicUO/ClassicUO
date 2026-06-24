# Network Packet Reference

UO classic-client packet docs. One file per packet, split by direction.

- `incoming/` — server → client. Mirrors `src/ClassicUO.Client/Ecs/Network/IncomingPackets/On*Packet_*.cs`. Each struct implements `IPacket.Fill(StackDataReader)`.
- `outgoing/` — client → server. Mirrors `Send_*` extension methods in `src/ClassicUO.Client/Network/OutgoingPackets.cs`.

## Doc template

Compact. Each file:

```
# 0xXX — PacketName

**Direction:** in | out
**Length:** fixed N bytes | dynamic (length-prefixed ushort)
**Source:** path/to/file.cs

## Fields

| Offset | Type | Name | Notes |
|--------|------|------|-------|
| ...    | ...  | ...  | ...   |

## Behavior

One-line summary of what the client does when it receives / sends this packet.
```

## Packet framing

- First byte is the packet id.
- If `PacketsTable.GetPacketLength(id) == -1` the next 2 bytes (big-endian) are total length including header. Otherwise length is fixed from the table.
- All multi-byte ints are big-endian unless noted.
- Strings: ASCII fixed-length (zero-padded) or Unicode UTF-16BE depending on packet.

## Pipeline

- **Recv**: `NetworkPlugin.PacketReader` (`NetworkPlugin.cs:250`) dequeues, calls registered `IPacket.Fill`, fans out via `EventWriter<IPacket>`. `InGamePacketsPlugin.HandlePacket` and `LoginPacketsPlugin` dispatch to handlers.
- **Send**: any system calls `network.Value.Send_X(...)`. `NetClient.Send` enqueues to the socket; `NetworkPlugin.PacketReader` flushes each tick.
- **Versioned variants**: handlers picked at startup from `gameCtx.ClientVersion` (see `NetworkPlugin.Build` registration block).

## Conventions

- `Serial` = 32-bit entity id. Bit 31 of the serial sometimes carries "has amount" / similar flags — see per-packet notes.
- `Graphic` high bit (0x8000) commonly signals "has graphic increment" byte follows.
- `X/Y` high bit (0x8000) commonly signals "has direction/hue".
- `Layer` enum: equipment slot index (1..24). See `ClassicUO.Game.Data.Layer`.
