# 0x3A — Send_SkillsStatusRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** (none in repo — likely host-side or mod-driven)

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| ushort | skillIndex | |
| byte | lockState | up/down/locked |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x3A | |
| u16  | skillIndex | |
| u8   | lockState | |

## Behavior

Sets skill lock state. See also 0x3A Send_SkillStatusChangeRequest.
