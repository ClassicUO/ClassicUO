# 0xF3 — UpdateItemSA

**Direction:** in
**Length:** 26 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u16 | (skipped) | |
| u8 | UpdateType | 2 = multi |
| u32 | Serial | |
| u16 | Graphic | |
| u8 | GraphicIncrement | |
| u16 | Amount | |
| u16 | Unknown1 | |
| u16 | X | |
| u16 | Y | |
| i8 | Z | |
| enum:Direction | Direction | u8 |
| u16 | Hue | |
| enum:Flags | Flags | u8 |
| u16 | Unknown2 | |

## Behavior

Spawns/updates game object (graphic/graphicInc/amount/x/y/z/dir/hue/flags/type/unk2) for non-player serials and auto-opens corpses when AutoOpenCorpses profile flag set on graphic 0x2006; when serial == player and parent packet is 0xF7, delegates to UpdatePlayer.
