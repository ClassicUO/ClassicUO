# 0x9A — Send_ASCIIPromptResponse

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/SystemChatControl.cs:560`, `src/ClassicUO.Client/Game/UI/Gumps/SystemChatControl.cs:598`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| World | world | reads MessageManager.PromptData |
| string | text | |
| bool | cancel | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x9A | |
| u16  | length | dynamic |
| u64  | prompt.Data | |
| u32  | cancel ? 0 : 1 | |
| ascii | text | null-terminated |

## Behavior

Replies to a server ASCII text prompt.
