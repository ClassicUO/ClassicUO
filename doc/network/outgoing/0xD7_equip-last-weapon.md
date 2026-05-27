# 0xD7 — Send_EquipLastWeapon

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/Managers/MacroManager.cs:1463`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| World | world | reads Player.Serial |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD7 | |
| u16  | length | dynamic |
| u32  | player serial | |
| u16  | 0x1E | subcommand: equip last weapon |
| u8   | 0x0A | |

## Behavior

Asks server to re-equip the previously held weapon.
