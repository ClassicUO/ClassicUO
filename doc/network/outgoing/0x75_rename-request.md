# 0x75 — Send_RenameRequest

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:663`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | pet/follower |
| string | name | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x75 | |
| u32  | serial | |
| ascii(30) | name | zero-padded |

## Behavior

Renames a pet / hireling.
