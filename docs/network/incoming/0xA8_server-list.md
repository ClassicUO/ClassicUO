# 0xA8 — ServerList

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Flags | |
| u16 | count | |
| list | Servers | per entry: u16 Index, ASCII(32) Name safe, u8 PercentFull, u8 TimeZone, u32 Address |

## Behavior

Routes to LoginScene.ServerListReceived which populates the shard list UI; ignored once in-game.
