# 0x89 — CorpseEquipment

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| list | Items | until Layer == Invalid (0) or serial == 0; each entry: u8 Layer, u32 Serial |

## Behavior

Iterates layer entries (terminated by Layer.Invalid), attaching each item serial to the corpse container at (layer-1); skips backpack layer; aborts if the entity is not a corpse (graphic 0x2006).
