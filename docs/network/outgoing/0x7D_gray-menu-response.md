# 0x7D — Send_GrayMenuResponse

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/MenuGump.cs:300`, `src/ClassicUO.Client/Game/UI/Gumps/MenuGump.cs:314`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |
| ushort | graphic | menu id |
| ushort | code | choice |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x7D | |
| u32  | serial | |
| u16  | graphic | |
| u16  | code | |

## Behavior

Selects an item from a text-only "gray" menu.
