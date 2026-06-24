# 0xB1 — Send_GumpResponse

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs:338`, `src/ClassicUO.Client/Game/GameActions.cs:553`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | local | gump serial |
| uint | server | gump type id |
| int | button | clicked button |
| uint[] | switches | checkbox states |
| Tuple<ushort,string>[] | entries | text input fields |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xB1 | |
| u16  | length | dynamic |
| u32  | local | |
| u32  | server | |
| u32  | button | |
| u32  | switchCount | |
| u32[] | switches | |
| u32  | entryCount | |
| (u16 id, u16 len, unicode(len)) per entry | clamped to 239 chars |

## Behavior

Submits a gump response back to server.
