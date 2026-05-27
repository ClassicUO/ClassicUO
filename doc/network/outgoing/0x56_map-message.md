# 0x56 — Send_MapMessage

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/MapGump.cs:120`, `src/ClassicUO.Client/Game/UI/Gumps/MapGump.cs:131`, `src/ClassicUO.Client/Game/UI/Gumps/MapGump.cs:190`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | map item |
| byte | action | add/insert/move/remove/clear/toggle |
| byte | pin | pin index |
| ushort | x | |
| ushort | y | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x56 | |
| u32  | serial | |
| u8   | action | |
| u8   | pin | |
| u16  | x | |
| u16  | y | |

## Behavior

Adds, moves, or removes a pin on a player map.
