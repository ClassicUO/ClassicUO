# 0xB8 — Send_ProfileRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs:355`, `src/ClassicUO.Client/Game/GameActions.cs:572`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xB8 | |
| u16  | length | dynamic |
| u8   | 0x00 | subcommand: read |
| u32  | serial | |

## Behavior

Requests a player's character profile text.
