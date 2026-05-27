# 0x12 — Send_OpenDoor

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:733`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x12 | |
| u16  | length | dynamic |
| u8   | 0x58 | subcommand |
| u8   | 0x00 | |

## Behavior

Auto-opens a nearby door in player's facing direction.
