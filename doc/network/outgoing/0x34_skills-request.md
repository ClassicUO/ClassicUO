# 0x34 — Send_SkillsRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs:299`, `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:434`, `src/ClassicUO.Client/Game/GameActions.cs:145`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | mobile |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x34 | |
| u32  | 0xEDEDEDED | pattern |
| u8   | 0x05 | subcommand: skills |
| u32  | serial | |

## Behavior

Requests the full skill list for serial.
