# 0xC7 — GraphicEffectC7

**Direction:** in
**Length:** 49 bytes

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
| u16 | TileId | |
| u16 | ExplodeEffect | |
| u16 | ExplodeSound | |
| u32 | ExtraSerial | |
| u8 | Layer | |
| u16 | (padding) | skipped |

## Behavior

Same as 0xC0 with extra tail (tileID, explodeEffect, explodeSound, serial, layer); spawns graphic effect between source and target with hue/blendmode.
