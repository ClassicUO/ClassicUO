#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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