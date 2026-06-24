# 0xF7 — PacketList

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u16 | Count | |
| list | PacketIds | Count entries of u8 |

## Behavior

Dispatches each contained sub-packet by id (currently only 0xF3 UpdateItemSA is handled; unknown ids abort).
