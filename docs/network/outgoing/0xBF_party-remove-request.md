# 0xBF — Send_PartyRemoveRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:416`, `src/ClassicUO.Client/Game/GameActions.cs:421`, `src/ClassicUO.Client/Game/UI/Gumps/PartyGump.cs:396`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | member to remove (0 to leave) |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x06 | subcommand: party |
| u8   | 0x02 | party-sub: remove |
| u32  | serial | |

## Behavior

Removes a member from the party or leaves it.
