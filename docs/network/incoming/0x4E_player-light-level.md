# 0x4E — PlayerLightLevel

**Direction:** in
**Length:** 6 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u8 | Level | |

## Behavior

When serial == player, clamps Level to 0x1E and writes Light.RealPersonal; mirrors to Light.Personal unless UseCustomLightLevel is set.
