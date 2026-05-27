# 0xBC — SeasonChange

**Direction:** in
**Length:** 3 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Season | |
| u8 | Music | season music index |

## Behavior

Stores OldSeason + OldMusicIndex and calls world.ChangeSeason(season, music); dead players keep Desolation; season > 4 normalizes to 0.
