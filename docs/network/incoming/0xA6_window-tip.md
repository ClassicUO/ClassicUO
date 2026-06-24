# 0xA6 — WindowTip

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Flags | |
| u32 | Serial | |
| u16 | TextLength | |
| string | Text | ASCII(TextLength) |

## Behavior

Opens TipNoticeGump with tip id and text body; flag 0 places at (200,100), flag != 0 at (20,20); flag 1 aborts.
