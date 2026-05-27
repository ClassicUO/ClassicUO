# 0x7D — Send_MenuResponse

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/MenuGump.cs:116`, `src/ClassicUO.Client/Game/UI/Gumps/MenuGump.cs:139`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |
| ushort | graphic | menu graphic |
| int | code | choice index |
| ushort | itemGraphic | |
| ushort | itemHue | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x7D | |
| u32  | serial | |
| u16  | graphic | |
| u16  | code | only if code != 0 |
| u16  | itemGraphic | only if code != 0 |
| u16  | itemHue | only if code != 0 |

## Behavior

Selects an item from a graphic NPC menu.
