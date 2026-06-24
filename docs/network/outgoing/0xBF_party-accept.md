# 0xBF — Send_PartyAccept

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:409`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | inviter |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x06 | subcommand: party |
| u8   | 0x08 | party-sub: accept |
| u32  | serial | |

## Behavior

Accepts a party invitation.
