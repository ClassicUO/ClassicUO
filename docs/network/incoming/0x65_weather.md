# 0x65 — Weather

**Direction:** in
**Length:** 4 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| enum:WeatherType | WeatherType | u8 |
| u8 | Count | particle count |
| u8 | Temperature | |

## Behavior

Generates weather (type/count/temp) on the world Weather manager when the type changed.
