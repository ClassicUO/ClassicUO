# 0x6C — Send_TargetObject

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/Managers/TargetManager.cs:276`, `src/ClassicUO.Client/Game/Managers/TargetManager.cs:332`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | entity | |
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
| u8   | 0x00 | targetMode: object |
| u32  | cursorID | |
| u8   | cursorType | |
| u32  | entity | |
| u16  | x | |
| u16  | y | |
| u16  | z | as u16 |
| u16  | graphic | |

## Behavior

Submits an entity pick to a server-issued targeting cursor.
