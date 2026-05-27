# 0xC2 — Send_UnicodePromptResponse

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/SystemChatControl.cs:564`, `src/ClassicUO.Client/Game/UI/Gumps/SystemChatControl.cs:602`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| World | world | reads MessageManager.PromptData |
| string | text | |
| string | lang | |
| bool | cancel | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xC2 | |
| u16  | length | dynamic |
| u64  | prompt.Data | |
| u32  | cancel ? 0 : 1 | |
| ascii(3) | lang | |
| u8   | 0x00 | |
| unicodeLE | text | |

## Behavior

Replies to a server unicode text prompt.
