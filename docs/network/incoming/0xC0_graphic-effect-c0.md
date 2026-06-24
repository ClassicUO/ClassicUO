# 0xC0 — GraphicEffectC0

**Direction:** in
**Length:** 36 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| enum:GraphicEffectType | EffectType | u8 |
| u32 | SourceSerial | |
| u32 | TargetSerial | |
| u16 | Graphic | |
| u16 | SourceX | |
| u16 | SourceY | |
| i8 | SourceZ | |
| u16 | TargetX | |
| u16 | TargetY | |
| i8 | TargetZ | |
| u8 | Speed | |
| u8 | Duration | |
| u16 | Unknown | |
| bool | FixedDirection | |
| bool | WillExplode | |
| u32 | Hue | |
| enum:GraphicEffectBlendMode | BlendMode | u32 BE |

## Behavior

Same as 0x70 but adds hue and blend mode: spawns graphic effect between source and target with hue/blendmode.
