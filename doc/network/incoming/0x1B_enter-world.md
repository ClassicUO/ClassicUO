# 0x1B — EnterWorld

**Direction:** in
**Length:** 37 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u32 | Unused0 | |
| u16 | Graphic | |
| tuple | Position | (u16 X, u16 Y, sbyte from u16 Z) |
| enum:Direction | Direction | u8 |
| u32 | Unused1 | |
| u32 | Unused2 | |
| u8 | Unused3 | |
| u16 | MapWidth | |
| u16 | MapHeight | |

## Behavior

Creates Player entity at serial with graphic/x/y/z/direction, seeds RangeSize, applies custom light overrides, updates music volume. Sends Send_GameWindowSize + Send_Language (>= CV_200), Send_ClientVersion, SingleClick(player), Send_SkillsRequest, Send_ShowPublicHouseContent (>= CV_70796), and Send_ToPlugins_AllSkills/AllSpells; dead players swap to Desolation season.
