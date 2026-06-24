# 0xBF — Send_ToPlugins_AllSpells

**Direction:** out (in-process to plugins via Plugin.ProcessRecvPacket)
**Length:** dynamic (length-prefixed ushort)
**Callers:** `src/ClassicUO.Client/Network/PacketHandlers.cs:953`

## Parameters

| C# Type | Name | Notes |
|---------|------|-------|

## Wire format

| Type | Value | Notes |
|------|-------|-------|
| u8   | ID = 0xBF | |
| u16  | length | dynamic |
| u16  | 0xBEEF | private subcommand |
| u8   | 0x00 | section: spells |
| per discipline (magery/necro/bushido/ninjitsu/chivalry/spellweaving/mastery): | | |
| u16  | count | |
| per spell: u16 id, u16 mana, u16 minSkill, u8 targetType, u16 nameLen, unicodeBE name, u16 wordsLen, unicodeBE words, u16 regCount, u8[] regs | | |

## Behavior

Pushes the full spell-definition table to in-process plugins (Razor-style) — does NOT go over the network. Uses `Plugin.ProcessRecvPacket`.
