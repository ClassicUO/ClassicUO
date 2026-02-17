// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    internal enum SpellBookType
    {
        Magery,
        Necromancy,
        Chivalry,
        Bushido = 4,
        Ninjitsu,
        Spellweaving,
        Mysticism,
        Mastery,
        // Vystia custom magic schools
        VystiaIceMagic = 10,
        VystiaDruid,
        VystiaWitch,
        VystiaSorcerer,
        VystiaWarlock,
        VystiaOracle,
        VystiaNecromancer,
        VystiaSummoner,
        VystiaShaman,
        VystiaBard,
        VystiaSongweaving,
        VystiaEnchanter,
        VystiaIllusionist,
        Unknown = 0xFF
    }

    internal static class SpellBookDefinition
    {
        #region MacroSubType Offsets
        // Offset for MacroSubType
        private const int MAGERY_SPELLS_OFFSET = 61;
        private const int NECRO_SPELLS_OFFSET = 125;
        private const int CHIVAL_SPELLS_OFFSETS = 142;
        private const int BUSHIDO_SPELLS_OFFSETS = 152;
        private const int NINJITSU_SPELLS_OFFSETS = 158;
        private const int SPELLWEAVING_SPELLS_OFFSETS = 166;
        private const int MYSTICISM_SPELLS_OFFSETS = 182;
        private const int MASTERY_SPELLS_OFFSETS = 198;
        // Vystia custom spells (1000-1383)
        private const int VYSTIA_ICE_MAGIC_SPELLS_OFFSET = 250;
        private const int VYSTIA_DRUID_SPELLS_OFFSET = 282;
        private const int VYSTIA_WITCH_SPELLS_OFFSET = 314;
        private const int VYSTIA_SORCERER_SPELLS_OFFSET = 346;
        private const int VYSTIA_WARLOCK_SPELLS_OFFSET = 378;
        private const int VYSTIA_ORACLE_SPELLS_OFFSET = 410;
        private const int VYSTIA_NECROMANCER_SPELLS_OFFSET = 442;
        private const int VYSTIA_SUMMONER_SPELLS_OFFSET = 474;
        private const int VYSTIA_SHAMAN_SPELLS_OFFSET = 506;
        private const int VYSTIA_BARD_SPELLS_OFFSET = 538;
        private const int VYSTIA_ENCHANTER_SPELLS_OFFSET = 570;
        private const int VYSTIA_ILLUSIONIST_SPELLS_OFFSET = 602;
        private const int VYSTIA_SONGWEAVING_SPELLS_OFFSET = 634;

        #endregion

        public static int GetSpellsGroup(int spellID)
        {
            var spellsGroup = spellID / 100;

            // Handle Vystia custom spells (1000-1415)
            if (spellID >= 1000)
            {
                if (spellID < 1032) return VYSTIA_ICE_MAGIC_SPELLS_OFFSET;
                if (spellID < 1064) return VYSTIA_DRUID_SPELLS_OFFSET;
                if (spellID < 1096) return VYSTIA_WITCH_SPELLS_OFFSET;
                if (spellID < 1128) return VYSTIA_SORCERER_SPELLS_OFFSET;
                if (spellID < 1160) return VYSTIA_WARLOCK_SPELLS_OFFSET;
                if (spellID < 1192) return VYSTIA_ORACLE_SPELLS_OFFSET;
                if (spellID < 1224) return VYSTIA_NECROMANCER_SPELLS_OFFSET;
                if (spellID < 1256) return VYSTIA_SUMMONER_SPELLS_OFFSET;
                if (spellID < 1288) return VYSTIA_SHAMAN_SPELLS_OFFSET;
                if (spellID < 1320) return VYSTIA_BARD_SPELLS_OFFSET;
                if (spellID < 1352) return VYSTIA_ENCHANTER_SPELLS_OFFSET;
                if (spellID < 1384) return VYSTIA_ILLUSIONIST_SPELLS_OFFSET;
                if (spellID < 1416) return VYSTIA_SONGWEAVING_SPELLS_OFFSET;
                return -1; // Unknown Vystia spell
            }

            switch (spellsGroup)
            {
                case (int)SpellBookType.Magery:
                    return MAGERY_SPELLS_OFFSET;
                case (int)SpellBookType.Necromancy:
                    return NECRO_SPELLS_OFFSET;
                case (int)SpellBookType.Chivalry:
                    return CHIVAL_SPELLS_OFFSETS;
                case (int)SpellBookType.Bushido:
                    return BUSHIDO_SPELLS_OFFSETS;
                case (int)SpellBookType.Ninjitsu:
                    return NINJITSU_SPELLS_OFFSETS;
                case (int)SpellBookType.Spellweaving:
                    // Mysticicsm Spells Id starts from 678 and Spellweaving ends at 618
                    if (spellID > 620)
                    {
                        return MYSTICISM_SPELLS_OFFSETS;
                    }
                    return SPELLWEAVING_SPELLS_OFFSETS;
                case (int)SpellBookType.Mastery - 1:
                    return MASTERY_SPELLS_OFFSETS;
            }
            return -1;
        }
    }
}
