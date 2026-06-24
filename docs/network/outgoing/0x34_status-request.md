# 0x34 — Send_StatusRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs:324`, `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:464`, `src/ClassicUO.Client/Game/GameActions.cs:607`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | mobile |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x34 | |
| u32  | 0xEDEDEDED | pattern |
| u8   | 0x04 | subcommand: status |
| u32  | serial | |

## Behavior

Requests the status bar packet (HP/mana/stamina) for serial.
