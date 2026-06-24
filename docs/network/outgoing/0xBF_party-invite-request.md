# 0xBF — Send_PartyInviteRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:431`, `src/ClassicUO.Client/Game/UI/Gumps/PartyGump.cs:349`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x06 | subcommand: party |
| u8   | 0x01 | party-sub: invite (target cursor) |
| u32  | 0 | |

## Behavior

Opens server-side target cursor for inviting a player to party.
