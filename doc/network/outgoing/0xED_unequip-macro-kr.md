# 0xED — Send_UnequipMacroKR

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** (none in repo — likely host-side or mod-driven)

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| ReadOnlySpan<Layer> | layers | layers to unequip |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xED | |
| u16  | length | dynamic |
| u8   | count | |
| u16[] | layers | each layer cast to u8 then written as u16BE |

## Behavior

KR-style batched unequip macro.
