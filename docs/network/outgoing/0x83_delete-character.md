# 0x83 — Send_DeleteCharacter

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/Scenes/LoginScene.cs:431`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| byte | index | character slot |
| uint | ipclient | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x83 | |
| 30 bytes | zero | password placeholder |
| u32  | index | |
| u32  | ipclient | |

## Behavior

Deletes a character slot.
