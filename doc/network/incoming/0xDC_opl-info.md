# 0xDC — OplInfo

**Direction:** in
**Length:** 9 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u32 | Revision | |

## Behavior

If tooltips enabled and cached OPL revision differs, queues a Send_MegaClilocRequest for serial via AddMegaClilocRequest.
