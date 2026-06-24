# 0xBF — Send_DisarmRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:756`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x0A | subcommand: disarm |

## Behavior

Legacy disarm special move request.
