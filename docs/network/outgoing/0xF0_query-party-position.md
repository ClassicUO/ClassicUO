# 0xF0 — Send_QueryPartyPosition

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/WorldMapEntityManager.cs:230`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xF0 | |
| u16  | length | dynamic |
| u8   | 0x00 | subcommand |

## Behavior

Asks server for live party member positions.
