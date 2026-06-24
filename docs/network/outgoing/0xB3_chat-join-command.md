# 0xB3 — Send_ChatJoinCommand

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Network/PacketHandlers.cs:3858`, `src/ClassicUO.Client/Game/UI/Gumps/ChatGump.cs:222`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | name | channel |
| string | password | optional |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xB3 | |
| u16  | length | dynamic |
| ascii(4) | lang | global settings language |
| u16  | 0x62 | command: join |
| u16  | 0x22 | open-quote |
| unicodeBE | name | |
| u16  | 0x22 | close-quote |
| u16  | 0x020 | space |
| unicodeBE | password | only if non-empty |

## Behavior

Joins a chat channel.
