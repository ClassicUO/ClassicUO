# 0x02 — Send_WalkRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/Movement/PlayerMovementPlugin.cs:320`, `src/ClassicUO.Client/Game/GameObjects/PlayerMobile.cs:1638`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| Direction | direction | |
| byte | seq | sequence id |
| bool | run | OR's Running flag into direction |
| uint | fastWalk | fast-walk prevention key |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x02 | |
| u8   | direction | with Running bit if run |
| u8   | seq | |
| u32  | fastWalk | |

## Behavior

Submits a step request — server ACKs / rejects.
