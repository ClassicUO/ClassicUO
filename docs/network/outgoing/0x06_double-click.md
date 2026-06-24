# 0x06 — Send_DoubleClick

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/Input/UseObjectPlugin.cs:40`, `src/ClassicUO.Client/Ecs/Scenes/GameScreenPlugin.cs:117`, `src/ClassicUO.Client/Game/GameActions.cs:308`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | target entity |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x06 | |
| u32  | serial | |

## Behavior

Server-side double-click on serial — opens paperdoll / container / use-effect.
