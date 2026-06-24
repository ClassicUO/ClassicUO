# 0xB5 — Send_OpenChat

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:465`, `src/ClassicUO.Client/Network/PacketHandlers.cs:2244`, `src/ClassicUO.Client/Game/UI/Gumps/ChatGumpChooseName.cs:156`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | name | chat name (max 30) |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xB5 | |
| u8   | 0x00 | |
| unicodeBE(<=30) | name | optional, omitted if empty |

## Behavior

Opens the chat window with a chosen identity name.
