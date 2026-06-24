# 0x6C — Send_TargetCancel

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/Managers/TargetManager.cs:193`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| CursorTarget | type | |
| uint | cursorID | |
| byte | cursorType | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x6C | |
| u8   | type | targetMode |
| u32  | cursorID | |
| u8   | cursorType | |
| u32  | 0 | |
| u32  | 0xFFFFFFFF | cancel sentinel |
| u32  | 0 | |

## Behavior

Cancels the active server target cursor.
