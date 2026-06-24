# 0x09 — Send_ClickRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:433`, `src/ClassicUO.Client/Game/GameActions.cs:324`, `src/ClassicUO.Client/Game/GameActions.cs:718`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x09 | |
| u32  | serial | |

## Behavior

Single-click on serial — server returns name overhead.
