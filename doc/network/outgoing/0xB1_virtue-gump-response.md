# 0xB1 — Send_VirtueGumpResponse

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UI/Controls/GumpPic.cs:161`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | player |
| uint | code | virtue id |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xB1 | |
| u16  | length | dynamic |
| u32  | serial | |
| u32  | 0x000001CD | virtue gump id |
| u32  | code | virtue button |

## Behavior

Invokes a virtue button on the virtue gump.
