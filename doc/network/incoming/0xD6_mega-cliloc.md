# 0xD6 — MegaCliloc

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u16 | Unknown | |
| u32 | Serial | |
| u16 | (skipped) | |
| u32 | Revision | |
| list | Entries | until cliloc==0; each: i32 Cliloc, u16 argLen, unicode LE(argLen/2) Argument |

## Behavior

Translates the cliloc list into the entity's OPL (name + tooltip data) keyed by revision; first entry becomes entity.Name; refreshes the open ShopGump name when item is in a vendor's buy list.
