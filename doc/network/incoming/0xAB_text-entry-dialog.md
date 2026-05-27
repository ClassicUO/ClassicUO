# 0xAB — TextEntryDialog

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u8 | ParentId | |
| u8 | ButtonId | |
| u16 | TextLength | |
| string | Text | ASCII(TextLength) |
| bool | ShowCancel | |
| u8 | Variant | |
| u32 | MaxLength | |
| u16 | DescriptionLength | |
| string | Description | ASCII(DescriptionLength) |

## Behavior

Opens TextEntryDialogGump with prefilled text/desc/maxLen/variant; right-click close enabled when haveCancel.
