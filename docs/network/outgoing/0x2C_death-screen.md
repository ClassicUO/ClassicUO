# 0x2C — Send_DeathScreen

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** (none in repo — likely host-side or mod-driven)

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x2C | |
| u8   | 0x02 | ghost choice |

## Behavior

Selects "Ghost" from the resurrection prompt.
