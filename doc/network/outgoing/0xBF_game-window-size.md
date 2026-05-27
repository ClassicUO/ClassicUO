# 0xBF — Send_GameWindowSize

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:428`, `src/ClassicUO.Client/Network/PacketHandlers.cs:923`, `src/ClassicUO.Client/Game/UI/Gumps/WorldViewportGump.cs:63`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | w | |
| uint | h | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0x05 | subcommand: window size |
| u32  | w | |
| u32  | h | |

## Behavior

Tells server the visible game window dimensions.
