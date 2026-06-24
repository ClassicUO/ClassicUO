# 0x71 — BulletinBoard

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | Type | 1=summary, 2=full message |
| u32 | BoardSerial | |
| u32? | MessageSerial | type 1 only |
| u32? | MessageParentSerial | type 1 only |
| string | MessagePreview | type 1: three length-prefixed ASCII chunks joined |
| string | Author | type 2: length-prefixed ASCII |
| string | Subject | type 2: length-prefixed ASCII |
| string | DateTime | type 2: length-prefixed ASCII |
| list | Lines | type 2: skip u32 msg id, u8 attach count + 4*N skip, u8 lineCount, lineCount length-prefixed ASCII |

## Behavior

Sub 0: opens BulletinBoardGump for item, marks Opened. Sub 1: adds summary entry (poster/subject/datetime) to board. Sub 2: opens BulletinBoardItem with full message body.
