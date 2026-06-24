# 0x55 — LoginComplete

**Direction:** in
**Length:** 1 byte (header-only)

## Fields

| Type | Name | Notes |
|------|------|-------|
| — | — | payload drained, no fields |

## Behavior

Swaps from LoginScene to GameScene, requests mobile status, sends Send_OpenChat(""), Send_SkillsRequest, double-clicks player, and (per client version) Send_ClientType (>= CV_306E) and Send_ClientViewRange (>= CV_305D); loads cached gumps from the profile.
