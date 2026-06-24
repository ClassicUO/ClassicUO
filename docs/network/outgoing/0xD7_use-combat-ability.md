# 0xD7 — Send_UseCombatAbility

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/GameActions.cs:760`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| World | world | reads Player.Serial |
| byte | idx | ability index |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xD7 | |
| u16  | length | dynamic |
| u32  | player serial | |
| u16  | 0x19 | subcommand: combat ability |
| u32  | 0 | |
| u8   | idx | |
| u8   | 0x0A | |

## Behavior

Activates a special combat ability (primary/secondary).
