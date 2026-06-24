# 0xD7 — Send_CustomHouseGoToFloor

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/HouseCustomizationGump.cs:1931`, `src/ClassicUO.Client/Game/UI/Gumps/HouseCustomizationGump.cs:1945`, `src/ClassicUO.Client/Game/UI/Gumps/HouseCustomizationGump.cs:1959`, `src/ClassicUO.Client/Game/UI/Gumps/HouseCustomizationGump.cs:1973`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| World | world | reads Player.Serial |
| byte | floor | 1..4 |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD7 | |
| u16  | length | dynamic |
| u32  | player serial | |
| u16  | 0x12 | subcommand: go to floor |
| u32  | 0 | |
| u8   | floor | |
| u8   | 0x0A | |

## Behavior

Custom house — switch design floor.
