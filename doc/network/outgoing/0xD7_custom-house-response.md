# 0xD7 — Send_CustomHouseResponse

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** (none in repo — likely host-side or mod-driven)

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
| u16  | 0x0A | subcommand: response/ack |
| u8   | 0x0A | |

## Behavior

Custom house — generic response/ack frame.
