# 0xBF — Send_PartyChangeLootTypeRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:436`, `src/ClassicUO.Client/Game/UI/Gumps/PartyGump.cs:280`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| bool | type | lootable flag |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x06 | subcommand: party |
| u8   | 0x06 | party-sub: change loot |
| bool | type | |

## Behavior

Toggles party-member-can-loot-me flag.
