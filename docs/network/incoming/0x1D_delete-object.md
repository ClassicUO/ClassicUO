# 0x1D — DeleteObject

**Direction:** in
**Length:** 5 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |

## Behavior

Removes the mobile or item; for equipped items refreshes the PaperDollGump; for container items refreshes ContainerGump/GridLootGump; disposes BulletinBoardItem on graphic 0x0EB0; updates player abilities when a OneHanded/TwoHanded item is removed. Skips when serial matches a corpse owner.
