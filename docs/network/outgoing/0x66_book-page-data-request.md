# 0x66 — Send_BookPageDataRequest

**Direction:** out
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Network/PacketHandlers.cs:3223`, `src/ClassicUO.Client/Game/UI/Gumps/ModernBookGump.cs:290`, `src/ClassicUO.Client/Game/UI/Gumps/ModernBookGump.cs:295`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|
| uint | serial | book |
| ushort | page | |

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0x66 | |
| u16  | length | dynamic |
| u32  | serial | |
| u16  | 0x01 | |
| u16  | page | |
| u16  | 0xFFFF | request sentinel |

## Behavior

Requests the text of a book page.
