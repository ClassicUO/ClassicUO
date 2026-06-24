# 0xD7 — Send_CustomHouseDeleteItem

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/HouseCustomizationManager.cs:663`

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
| u16  | 0x05 | subcommand: delete item |
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

Custom house — remove an item tile.
