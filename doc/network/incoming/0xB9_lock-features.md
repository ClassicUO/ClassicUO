# 0xB9 — LockFeatures

**Direction:** in
**Length:** fixed (2 or 4 bytes depending on remaining)

## Fields

| Type | Name | Notes |
|------|------|-------|
| enum:LockedFeatureFlags | Flags | u32 BE if remaining >= 4, else u16 BE |
| enum:BodyConvFlags | BodyConversionFlags | derived from Flags (UOR/LBR/AOS/SE/ML) |

## Behavior

Sets ClientLockedFeatures flags, toggles ChatStatus on T2A flag, and updates animation BodyConvFlags (UOR/LBR/AOS/SE/ML) so the animation table swaps to the right body-conversion set.
