# 0xD7 — Send_CustomHouseAddItem

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/HouseCustomizationManager.cs:786`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| World | world | reads Player.Serial |
| ushort | graphic | |
| int | x | relative to foundation |
| int | y | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD7 | |
| u16  | length | dynamic |
| u32  | player serial | |
| u16  | 0x06 | subcommand: add item |
| u8   | 0x00 | |
| u32  | graphic | |
| u8   | 0x00 | |
| u32  | x | |
| u8   | 0x00 | |
| u32  | y | |
| u8   | 0x0A | |

## Behavior

Custom house — place an item tile.
