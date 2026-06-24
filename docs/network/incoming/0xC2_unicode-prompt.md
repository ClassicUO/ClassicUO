# 0xC2 — UnicodePrompt

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u32 | MessageId | |
| bytes | RemainingData | captures rest of payload |

## Behavior

Arms a Unicode prompt on MessageManager.PromptData with the server's 8-byte token.
