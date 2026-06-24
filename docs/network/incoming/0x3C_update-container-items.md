# 0x3C — UpdateContainerItems

**Direction:** in
**Length:** dynamic (length-prefixed ushort)

## Fields

| Type | Name | Notes |
|------|------|-------|
| u16 | Count | |
| bytes | ItemsData | remaining bytes captured; parsed by version variants |

## Behavior

For each entry: on first iteration clears the container's prior contents (corpse-aware), then adds the item under its containerSerial with graphic/amount/hue/x/y.
