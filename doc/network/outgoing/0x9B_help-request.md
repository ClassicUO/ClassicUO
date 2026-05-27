# 0x9B — Send_HelpRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs:266`, `src/ClassicUO.Client/Game/GameActions.cs:562`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x9B | |
| 257 bytes | zero | reserved |

## Behavior

Asks server to open the GM help menu.
