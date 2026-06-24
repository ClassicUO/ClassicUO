# 0x6E — CharacterAnimation

**Direction:** in
**Length:** 14 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Action | |
| u16 | FrameCount | |
| u16 | RepeatForNTimes | |
| bool | Backward | |
| bool | Loop | |
| u8 | Delay | |

## Behavior

Plays animation (action/frameCount/repeatCount/forward/repeat/delay) on the target mobile.
