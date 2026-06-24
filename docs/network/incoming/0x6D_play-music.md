# 0x6D — PlayMusic

**Direction:** in
**Length:** 3 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u16 | Index | track id |

## Behavior

Plays music track by index; 3-byte midi form 0x6D 0x1F 0xFF stops music, others start the given index.
