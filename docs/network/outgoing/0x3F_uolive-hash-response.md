# 0x3F — Send_UOLive_HashResponse

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Game/UltimaLive.cs:167`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | block | map block id |
| byte | mapIndex | |
| Span<ushort> | checksums | per-row CRCs |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x3F | |
| u16  | length | dynamic |
| u32  | block | |
| 6 bytes | zero | |
| u8   | 0xFF | UltimaLive magic |
| u8   | mapIndex | |
| u16[] | checksums | |

## Behavior

UltimaLive map-tile sync — sends block CRCs so server pushes deltas.
