# 0xD7 — Send_CustomHouseCommit

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/HouseCustomizationGump.cs:2035`

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
| u16  | 0x04 | subcommand: commit |
| u8   | 0x0A | |

## Behavior

Custom house — commit pending edits to the live design.
