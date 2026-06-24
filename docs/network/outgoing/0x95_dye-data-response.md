# 0x95 — Send_DyeDataResponse

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/ColorPickerGump.cs:79`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | dye tub |
| ushort | graphic | unused (written 0) |
| ushort | hue | chosen hue |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x95 | |
| u32  | serial | |
| u16  | 0 | |
| u16  | hue | |

## Behavior

Sends chosen dye tub hue back to server.
