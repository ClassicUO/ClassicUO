# 0x98 — Send_NameRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameObjects/Entity.cs:139`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x98 | |
| u16  | length | dynamic |
| u32  | serial | |

## Behavior

Requests display name for a mobile.
