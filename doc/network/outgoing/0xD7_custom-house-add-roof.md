# 0xD7 — Send_CustomHouseAddRoof

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/HouseCustomizationManager.cs:782`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| World | world | reads Player.Serial |
| ushort | graphic | |
| int | x | |
| int | y | |
| int | z | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD7 | |
| u16  | length | dynamic |
| u32  | player serial | |
| u16  | 0x13 | subcommand: add roof |
| u8   | 0x00 | |
| u32  | graphic | |
| u8   | 0x00 | |
| u32  | x | |
| u8   | 0x00 | |
| u32  | y | |
| u8   | 0x00 | |
| u32  | z | |
| u8   | 0x0A | |

## Behavior

Custom house — place a roof tile.
