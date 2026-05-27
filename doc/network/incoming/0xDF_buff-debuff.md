# 0xDF — BuffDebuff

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| enum:BuffIconType | IconType | u16 BE |
| u16 | Count | |
| list | Entries | per entry: u16 SourceType, skip 2, u16 Icon, u16 QueueIndex, skip 4, u16 Timer, skip 3, u32 TitleCliloc, u32 DescriptionCliloc, u32 AdditionalCliloc, unicode args block + optional u16 Arguments2/Arguments3 |

## Behavior

Count == 0 removes the buff icon; otherwise translates title/description/wtf clilocs and adds buff to player (icon/timer/tooltip text) and refreshes BuffGump.
