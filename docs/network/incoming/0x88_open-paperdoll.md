# 0x88 — OpenPaperdoll

**Direction:** in
**Length:** 66 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| string(60) | Title | ASCII |
| u8 | Flags | |

## Behavior

Writes mobile.Title from the 60-byte text. Opens a new PaperDollGump for the mobile when none exists (CanLift from flag 0x02); otherwise updates the title, toggles CanLift, refreshes contents on change, and brings the existing gump to the top.
