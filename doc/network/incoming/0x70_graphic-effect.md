# 0x70 — GraphicEffect

**Direction:** in
**Length:** 28 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| enum:GraphicEffectType | EffectType | u8 |
| u64 | UnknownHeader | |
| u16 | UnknownValue | |
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

## Behavior

Spawns graphic effect (Moving/Lightning/FixedXY/FixedFrom) between source and target with speed/duration/fixedDirection/explode flags.
