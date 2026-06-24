# 0xB8 — OpenCharacterProfile

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| string | Header | ASCII null-terminated |
| string | Footer | unicode BE null-terminated |
| string | Body | unicode BE null-terminated |

## Behavior

Disposes any prior ProfileGump for serial and opens a new one with header/footer/body; editable only when serial == player.
