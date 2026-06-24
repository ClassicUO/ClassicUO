# 0xEC — Send_EquipMacroKR

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** (none in repo — likely host-side or mod-driven)

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| ReadOnlySpan<uint> | serials | items to equip |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xEC | |
| u16  | length | dynamic |
| u8   | count | |
| u32[] | serials | |

## Behavior

KR-style batched equip macro.
