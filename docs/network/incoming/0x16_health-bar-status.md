# 0x16 — HealthBarStatus

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Count | |
| list | Attributes | per entry: u16 Type, bool Enabled |

## Behavior

Toggles status flags on the target mobile per entry: type 1 = Poisoned (SA poison post-7000), type 2 = YellowBar. Pre-CV_500A 0x16 is ignored.
