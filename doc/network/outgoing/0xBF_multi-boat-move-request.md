# 0xBF — Send_MultiBoatMoveRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/BoatMovingManager.cs:49`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | boat |
| Direction | dir | |
| byte | speed | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x33 | subcommand: boat move |
| u32  | serial | |
| u8   | dir | movement |
| u8   | dir | facing (same) |
| u8   | speed | |

## Behavior

Drives a galleon / multi-tile boat.
