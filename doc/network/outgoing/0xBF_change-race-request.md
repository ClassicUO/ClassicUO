# 0xBF — Send_ChangeRaceRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/RaceChangeGump.cs:441`, `src/ClassicUO.Client/Game/UI/Gumps/RaceChangeGump.cs:451`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| ushort | skinHue | |
| ushort | hairStyle | |
| ushort | hairHue | |
| ushort | beardStyle | |
| ushort | beardHue | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x2A | subcommand: race change |
| u16  | skinHue | |
| u16  | hairStyle | |
| u16  | hairHue | |
| u16  | beardStyle | |
| u16  | beardHue | |

## Behavior

Submits race-change customizations to server.
