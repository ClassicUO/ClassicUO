# 0x03 — Send_ACKTalk

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:527`, `src/ClassicUO.Client/Network/PacketHandlers.cs:986`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x03 | |
| u16  | length | dynamic |
| u8 x36 | fixed magic payload | hardcoded bytes |

## Behavior

ACK talk handshake sent in response to server prompt — emits a fixed magic byte sequence.
