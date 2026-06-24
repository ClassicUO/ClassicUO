# 0xAD — Send_UnicodeSpeechRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/Chat/ChatPlugin.cs:57`, `src/ClassicUO.Client/Game/GameActions.cs:349`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | text | |
| MessageType | type | OR'd with Encoded if entries non-empty |
| byte | font | |
| ushort | hue | |
| string | lang | ISO 4-char tag |
| List<SpeechEntry> | entries | optional keyword triggers |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xAD | |
| u16  | length | dynamic |
| u8   | type | |
| u16  | hue | |
| u16  | font | |
| ascii(4) | lang | |
| varies | text | encoded keyword stream + utf8, OR unicodeBE; trailing null in encoded path |

## Behavior

Unicode chat / command with optional encoded keyword triggers.
