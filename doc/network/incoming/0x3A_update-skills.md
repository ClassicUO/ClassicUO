# 0x3A — UpdateSkills

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | UpdateType | 0xFE = skill definitions, otherwise values |
| list | Definitions | when UpdateType==0xFE: u16 count, per entry: bool HasButton, i8 nameLen, ASCII Name |
| list | Values | otherwise read while remaining > 0: i16 Id, i16 RealValue, i16 BaseValue, u8 Status, i16? Cap when HasCap |
| bool | HasCap | derived: UpdateType in {0x01..0x03} or 0xDF |
| bool | IsSingleUpdate | derived: UpdateType == 0xFF or 0xDF (stop after first entry) |

## Behavior

UpdateType 0xFE rebuilds the global skill definitions table (sorted by name); otherwise writes each Skill (Base/Value/Cap/Lock) on the player and refreshes the open SkillsGump (Standard or Advanced per profile), printing a delta message on single-skill updates.
