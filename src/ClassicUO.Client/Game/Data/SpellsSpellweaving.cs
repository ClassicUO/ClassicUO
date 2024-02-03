#region license

// Copyright (c) 2024, andreakarasho
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

using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class SpellsSpellweaving
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        static SpellsSpellweaving()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1,
                    new SpellDefinition
                    (
                        "Arcane Circle",
                        601,
                        0x59D8,
                        "Myrshalee",
                        20,
                        0,
                        TargetType.Neutral,
                        Reagents.None
                    )
                },
                {
                    2,
                    new SpellDefinition
                    (
                        "Gift of Renewal",
                        602,
                        0x59D9,
                        "Olorisstra",
                        24,
                        0,
                        TargetType.Beneficial,
                        Reagents.None
                    )
                },
                {
                    3,
                    new SpellDefinition
                    (
                        "Immolating Weapon",
                        603,
                        0x59DA,
                        "Thalshara",
                        32,
                        10,
                        TargetType.Neutral,
                        Reagents.None
                    )
                },
                {
                    4,
                    new SpellDefinition
                    (
                        "Attune Weapon",
                        604,
                        0x59DB,
                        "Haeldril",
                        24,
                        0,
                        TargetType.Harmful,
                        Reagents.None
                    )
                },
                {
                    5,
                    new SpellDefinition
                    (
                        "Thunderstorm",
                        605,
                        0x59DC,
                        "Erelonia",
                        32,
                        10,
                        TargetType.Harmful,
                        Reagents.None
                    )
                },
                {
                    6,
                    new SpellDefinition
                    (
                        "Nature's Fury",
                        606,
                        0x59DD,
                        "Rauvvrae",
                        24,
                        0,
                        TargetType.Neutral,
                        Reagents.None
                    )
                },
                {
                    7,
                    new SpellDefinition
                    (
                        "Summon Fey",
                        607,
                        0x59DE,
                        "Alalithra",
                        10,
                        38,
                        TargetType.Neutral,
                        Reagents.None
                    )
                },
                {
                    8,
                    new SpellDefinition
                    (
                        "Summon Fiend",
                        608,
                        0x59DF,
                        "Nylisstra",
                        10,
                        38,
                        TargetType.Neutral,
                        Reagents.None
                    )
                },
                {
                    9,
                    new SpellDefinition
                    (
                        "Reaper Form",
                        609,
                        0x59E0,
                        "Tarisstree",
                        34,
                        24,
                        TargetType.Neutral,
                        Reagents.None
                    )
                },
                {
                    10,
                    new SpellDefinition
                    (
                        "Wildfire",
                        610,
                        0x59E1,
                        "Haelyn",
                        50,
                        66,
                        TargetType.Harmful,
                        Reagents.None
                    )
                },
                {
                    11,
                    new SpellDefinition
                    (
                        "Essence of Wind",
                        611,
                        0x59E2,
                        "Anathrae",
                        40,
                        52,
                        TargetType.Harmful,
                        Reagents.None
                    )
                },
                {
                    12,
                    new SpellDefinition
                    (
                        "Dryad Allure",
                        612,
                        0x59E3,
                        "Rathril",
                        40,
                        52,
                        TargetType.Neutral,
                        Reagents.None
                    )
                },
                {
                    13,
                    new SpellDefinition
                    (
                        "Ethereal Voyage",
                        613,
                        0x59E4,
                        "Orlavdra",
                        32,
                        24,
                        TargetType.Neutral,
                        Reagents.None
                    )
                },
                {
                    14,
                    new SpellDefinition
                    (
                        "Word of Death",
                        614,
                        0x59E5,
                        "Nyraxle",
                        50,
                        23,
                        TargetType.Harmful,
                        Reagents.None
                    )
                },
                {
                    15,
                    new SpellDefinition
                    (
                        "Gift of Life",
                        615,
                        0x59E6,
                        "Illorae",
                        70,
                        38,
                        TargetType.Beneficial,
                        Reagents.None
                    )
                },
                {
                    16,
                    new SpellDefinition
                    (
                        "Arcane Empowerment",
                        616,
                        0x59E7,
                        "Aslavdra",
                        50,
                        24,
                        TargetType.Beneficial,
                        Reagents.None
                    )
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Spellweaving.ToString();

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;

        public static SpellDefinition GetSpell(int spellIndex)
        {
            if (_spellsDict.TryGetValue(spellIndex, out SpellDefinition spell))
            {
                return spell;
            }

            return SpellDefinition.EmptySpell;
        }

        public static void SetSpell(int id, in SpellDefinition newspell)
        {
            _spellsDict[id] = newspell;
        }

        internal static void Clear()
        {
            _spellsDict.Clear();
        }
    }
}