# 0xD4 — OpenBookAlt

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| bool | FirstFlag | |
| bool | IsEditable | |
| u16 | PageCount | |
| string | Title | u16 length-prefixed ASCII |
| string | Author | u16 length-prefixed ASCII |

## Behavior

Opens ModernBookGump (new-header variant with length-prefixed title/author); on new gump sends BookPageDataRequest for page 1; existing gump updates title/author/editable.
