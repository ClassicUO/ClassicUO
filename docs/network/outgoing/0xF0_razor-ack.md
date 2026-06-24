# 0xF0 — Send_RazorACK

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Network/PacketHandlers.cs:5687`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xF0 | |
| u16  | length | dynamic |
| u8   | 0xFF | subcommand: razor handshake ack |

## Behavior

Acknowledges Razor / KUOC handshake from server.
