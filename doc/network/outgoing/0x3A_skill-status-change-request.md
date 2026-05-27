# 0x3A — Send_SkillStatusChangeRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:577`, `src/ClassicUO.Client/Game/UI/Gumps/StandardSkillsGump.cs:852`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| ushort | skillindex | |
| byte | lockstate | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x3A | |
| u16  | skillindex | |
| u8   | lockstate | |

## Behavior

Sets skill lock state (up/down/locked).
