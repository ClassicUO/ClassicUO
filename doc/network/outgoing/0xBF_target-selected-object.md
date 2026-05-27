# 0xBF — Send_TargetSelectedObject

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/MacroManager.cs:1321`, `src/ClassicUO.Client/Game/Managers/MacroManager.cs:1325`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | item used (e.g. bandage) |
| uint | targetSerial | applied target |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x2C | subcommand: targeted use |
| u32  | serial | |
| u32  | targetSerial | |

## Behavior

Use-item-on-target macro (bandage-self, etc.).
