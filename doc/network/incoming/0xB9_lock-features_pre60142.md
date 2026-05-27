# 0xB9 — LockFeatures

**Direction:** in
**Length:** 3 bytes
**Variant:** Pre60142 — used when ClientVersion < 6.0.14.2

## Fields

| Type | Name | Notes |
|------|------|-------|
| enum:LockedFeatureFlags | Flags | u16 BE |

## Behavior

Sets ClientLockedFeatures flags, toggles ChatStatus on T2A, and updates animation BodyConvFlags (UOR/LBR/AOS/SE/ML) so the animation table swaps to the right body-conversion set.
