# 0x8C — ServerRelay

**Direction:** in
**Length:** 11 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Ip | LE |
| u16 | Port | |
| u32 | Seed | |

## Behavior

Routes to LoginScene.HandleRelayServerPacket which disconnects from the login shard and connects to the relayed game server (re-seeds encryption, sends seed + SecondLogin); ignored once in-game.
