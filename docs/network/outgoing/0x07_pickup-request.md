# 0x07 — Send_PickUpRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/Input/PickupPlugin.cs:552`, `src/ClassicUO.Client/Game/GameActions.cs:494`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | item |
| ushort | count | stack amount |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x07 | |
| u32  | serial | |
| u16  | count | |

## Behavior

Asks server to pick up `count` from item `serial` into the client's drag layer.
