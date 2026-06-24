# 0x93 — OpenBook

**Direction:** in
**Length:** 99 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| bool | FirstFlag | |
| u8 | (skipped) | |
| (alias) | IsEditable | == FirstFlag |
| u16 | PageCount | |
| string(60) | Title | ASCII safe |
| string(30) | Author | ASCII safe |

## Behavior

Opens ModernBookGump (old-header variant) for serial; on new gump immediately sends BookPageDataRequest for page 1; existing gump updates title/author/editable.
