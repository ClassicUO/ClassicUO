# 0x9A — AsciiPrompt

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u32 | PromptId | |
| u32 | Type | |
| string | Text | ASCII null-terminated |

## Behavior

Arms an ASCII prompt on MessageManager.PromptData with the server's 8-byte token.
