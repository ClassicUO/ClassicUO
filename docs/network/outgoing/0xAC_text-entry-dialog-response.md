# 0xAC — Send_TextEntryDialogResponse

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/TextEntryDialogGump.cs:99`, `src/ClassicUO.Client/Game/UI/Gumps/TextEntryDialogGump.cs:110`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | |
| byte | parentID | |
| byte | button | |
| string | text | |
| bool | code | OK/Cancel |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xAC | |
| u16  | length | dynamic |
| u32  | serial | |
| u8   | parentID | |
| u8   | button | |
| bool | code | |
| u16  | textLen+1 | |
| ascii(textLen+1) | text | null-terminated |

## Behavior

Submits text input from a server-issued text dialog.
