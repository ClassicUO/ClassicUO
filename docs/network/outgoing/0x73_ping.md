# 0x73 — Send_Ping

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Network/NetworkPlugin.cs:184`, `src/ClassicUO.Client/Network/NetStatistics.cs:79`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| byte | idx | ping sequence index |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x73 | |
| u8   | idx | |

## Behavior

Keep-alive ping; server echoes back same idx.
