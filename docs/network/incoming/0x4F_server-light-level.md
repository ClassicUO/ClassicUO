# 0x4F — ServerLightLevel

**Direction:** in
**Length:** 2 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Level | |

## Behavior

Clamps Level to 0x1E and writes Light.RealOverall; mirrors to Light.Overall (respecting profile LightLevelType=1 minimum cap) unless UseCustomLightLevel is set.
