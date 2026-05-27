# 0xD7 — Send_CustomHouseBuildingExit

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/HouseCustomizationGump.cs:2085`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| World | world | reads Player.Serial |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD7 | |
| u16  | length | dynamic |
| u32  | player serial | |
| u16  | 0x0C | subcommand: exit design mode |
| u8   | 0x0A | |

## Behavior

Custom house — leave design mode.
