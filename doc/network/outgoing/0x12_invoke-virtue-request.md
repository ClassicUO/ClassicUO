# 0x12 — Send_InvokeVirtueRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/MacroManager.cs:1439`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| byte | id | virtue id |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x12 | |
| u16  | length | dynamic |
| u8   | 0xF4 | subcommand: invoke virtue |
| ascii | id.ToString() | null-terminated |

## Behavior

Invokes a virtue (compassion, valor, etc.).
