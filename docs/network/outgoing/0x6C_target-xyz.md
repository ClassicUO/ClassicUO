# 0x6C — Send_TargetXYZ

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/Managers/TargetManager.cs:482`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| ushort | graphic | |
| ushort | x | |
| ushort | y | |
| sbyte | z | |
| uint | cursorID | |
| byte | cursorType | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x6C | |
| u8   | 0x01 | targetMode: tile |
| u32  | cursorID | |
| u8   | cursorType | |
| u32  | 0 | entity slot |
| u16  | x | |
| u16  | y | |
| u16  | z | |
| u16  | graphic | |

## Behavior

Submits a tile pick to a server-issued targeting cursor.
