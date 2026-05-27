# 0x13 — Send_EquipRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/Input/PickupPlugin.cs:694`, `src/ClassicUO.Client/Game/GameActions.cs:544`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | held item |
| Layer | layer | equipment slot |
| uint | container | mobile to equip on |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x13 | |
| u32  | serial | |
| u8   | layer | |
| u32  | container | |

## Behavior

Equips held item onto a mobile at the given layer.
