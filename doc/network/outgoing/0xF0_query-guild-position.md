# 0xF0 — Send_QueryGuildPosition

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/WorldMapEntityManager.cs:218`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xF0 | |
| u16  | length | dynamic |
| u8   | 0x01 | subcommand |
| bool | true | |

## Behavior

Asks server for live guild member positions (used by EasyUO-style world map).
