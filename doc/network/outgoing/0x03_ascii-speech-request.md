# 0x03 — Send_ASCIISpeechRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/Chat/ChatPlugin.cs:68`, `src/ClassicUO.Client/Game/GameActions.cs:358`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | text | |
| MessageType | type | OR'd with Encoded if entries non-empty |
| byte | font | |
| ushort | hue | |
| List<SpeechEntry> | entries | optional triggers |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x03 | |
| u16  | length | dynamic |
| u8   | type | |
| u16  | hue | |
| u16  | font | |
| ascii | text | null-terminated |

## Behavior

Pre-unicode ASCII chat / command speech.
