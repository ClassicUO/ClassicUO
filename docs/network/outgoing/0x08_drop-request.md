# 0x08 — Send_DropRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/Input/PickupPlugin.cs:714`, `src/ClassicUO.Client/Ecs/Gameplay/Input/PickupPlugin.cs:812`, `src/ClassicUO.Client/Game/GameActions.cs:514`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |
| ushort | x | |
| ushort | y | |
| sbyte | z | |
| byte | slot | container grid slot |
| uint | container | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x08 | |
| u32  | serial | |
| u16  | x | |
| u16  | y | |
| i8   | z | |
| u8   | slot | grid slot |
| u32  | container | |

## Behavior

Drops the held item at world/container coords (>=6.0.1.7 with grid slot).
