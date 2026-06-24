# 0xBF — Send_ToPlugins_AllSkills

**Direction:** out (in-process to plugins via Plugin.ProcessRecvPacket)
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Network/PacketHandlers.cs:952`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0xBEEF | private subcommand |
| u8   | 0x01 | section: skills |
| u16  | count | |
| per skill: u16 index, bool hasAction, u16 nameLen, unicodeBE name | | |

## Behavior

Pushes the full skill table to in-process plugins. Uses `Plugin.ProcessRecvPacket` — not sent over the network.
