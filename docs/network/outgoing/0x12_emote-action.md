# 0x12 — Send_EmoteAction

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:738`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| string | action | animation name |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x12 | |
| u16  | length | dynamic |
| u8   | 0xC7 | subcommand |
| ascii | action | null-terminated |

## Behavior

Plays an emote animation.
