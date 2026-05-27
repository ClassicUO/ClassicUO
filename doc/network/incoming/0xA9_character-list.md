# 0xA9 — CharacterList

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u8 | characterCount | |
| list | Characters | per slot: ASCII(30) name, skip(30); empty slots dropped |
| u8 | CityCount | |
| bytes | CityData | remaining minus 4 bytes; parsed later by `ParseTowns` (CV_70130 picks new format) |
| enum:CharacterListFlags | Flags | u32 BE tail |

## Behavior

Routes to LoginScene.ReceiveCharacterList which populates the character + starting city list UI; ignored once in-game.
