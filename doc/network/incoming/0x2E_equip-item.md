# 0x2E — EquipItem

**Direction:** in
**Length:** 15 bytes

## Fields

| Type | Name | Notes |
|------|------|-------|
| u32 | Serial | |
| u16 | Graphic | |
| u8 | GraphicIncrement | |
| enum:Layer | Layer | u8 |
| u32 | ContainerSerial | wearer |
| u16 | Hue | |

## Behavior

Removes item from any prior container, sets Graphic/Layer/Container/Hue/Amount and re-parents to the wearer; refreshes ContainerGump/PaperDollGump and triggers Player.UpdateAbilities when player equips a OneHanded/TwoHanded item.
