# 0xA7 — Send_TipRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/UI/Gumps/TipNoticeGump.cs:71`, `src/ClassicUO.Client/Game/UI/Gumps/TipNoticeGump.cs:77`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| ushort | id | tip/notice id |
| byte | flag | direction |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xA7 | |
| u16  | id | |
| u8   | flag | |

## Behavior

Requests a "tip of the day" / notice text.
