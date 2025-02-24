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

        #endregion

        public static int GetSpellsGroup(int spellID)
        {
            var spellsGroup = spellID / 100;

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