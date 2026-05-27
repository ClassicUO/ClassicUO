# 0x08 — Send_DropRequest_Old

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:523`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | held item |
| ushort | x | |
| ushort | y | |
| sbyte | z | |
| uint | container | drop target |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x08 | |
| u32  | serial | |
| u16  | x | |
| u16  | y | |
| i8   | z | |
| u32  | container | |

## Behavior

Pre-6.0.1.7 drop — no grid-slot byte.
