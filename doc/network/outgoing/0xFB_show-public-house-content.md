# 0xFB — Send_ShowPublicHouseContent

**Direction:** out
**Length:** fixed N bytes (per PacketsTable)
**Callers:** `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs:437`, `src/ClassicUO.Client/Network/PacketHandlers.cs:947`, `src/ClassicUO.Client/Game/UI/Gumps/OptionsGump.cs:3812`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| bool | show | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xFB | |
| bool | show | |

## Behavior

Toggles whether interior of public houses is rendered.
