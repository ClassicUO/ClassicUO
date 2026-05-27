# 0x95 — ColorPicker

**Direction:** in
**Length:** 9 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | (unknown) | skipped |
| u16 | Graphic | |

## Behavior

Opens ColorPickerGump centered on screen for serial+graphic; disposes prior gump if graphic differs.
