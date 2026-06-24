# 0xBF — Send_PartyDecline

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/PartyInviteGump.cs:77`, `src/ClassicUO.Client/Game/UI/Gumps/SystemChatControl.cs:736`

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
| u8   | 0x09 | party-sub: decline |
| u32  | serial | |

## Behavior

Rejects a party invitation.
