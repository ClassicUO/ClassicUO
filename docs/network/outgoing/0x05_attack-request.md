# 0x05 — Send_AttackRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:278`, `src/ClassicUO.Client/Game/GameActions.cs:291`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | target mobile |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x05 | |
| u32  | serial | |

## Behavior

Initiates combat against target mobile.
