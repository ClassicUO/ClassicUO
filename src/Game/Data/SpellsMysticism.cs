﻿#region license

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

using System.Collections.Generic;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class SpellsMysticism
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        static SpellsMysticism()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1,
                    new SpellDefinition
                    (
                        "Nether Bolt",
                        678,
                        0x5DC0,
                        "In Corp Ylem",
                        4,
                        0,
                        TargetType.Harmful,
                        Reagents.BlackPearl,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    2,
                    new SpellDefinition
                    (
                        "Healing Stone",
                        679,
                        0x5DC1,
                        "Kal In Mani",
                        4,
                        0,
                        TargetType.Neutral,
                        Reagents.Bone,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.SpidersSilk
                    )
                },
                {
                    3,
                    new SpellDefinition
                    (
                        "Purge Magic",
                        680,
                        0x5DC2,
                        "An Ort Sanct",
                        6,
                        8,
                        TargetType.Beneficial,
                        Reagents.FertileDirt,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    4,
                    new SpellDefinition
                    (
                        "Enchant",
                        681,
                        0x5DC3,
                        "In Ort Ylem",
                        6,
                        8,
                        TargetType.Neutral,
                        Reagents.SpidersSilk,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    5,
                    new SpellDefinition
                    (
                        "Sleep",
                        682,
                        0x5DC4,
                        "In Zu",
                        8,
                        20,
                        TargetType.Harmful,
                        Reagents.Nightshade,
                        Reagents.SpidersSilk,
                        Reagents.BlackPearl
                    )
                },
                {
                    6,
                    new SpellDefinition
                    (
                        "Eagle Strike",
                        683,
                        0x5DC5,
                        "Kal Por Xen",
                        9,
                        20,
                        TargetType.Harmful,
                        Reagents.Bloodmoss,
                        Reagents.Bone,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    7,
                    new SpellDefinition
                    (
                        "Animated Weapon",
                        684,
                        0x5DC6,
                        "In Jux Por Ylem",
                        11,
                        33,
                        TargetType.Neutral,
                        Reagents.Bone,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade
                    )
                },
                {
                    8,
                    new SpellDefinition
                    (
                        "Stone Form",
                        685,
                        0x5DC7,
                        "In Rel Ylem",
                        11,
                        33,
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.FertileDirt,
                        Reagents.Garlic
                    )
                },
                {
                    9,
                    new SpellDefinition
                    (
                        "Spell Trigger",
                        686,
                        0x5DC8,
                        "In Vas Ort Ex",
                        14,
                        45,
                        TargetType.Neutral,
                        Reagents.DragonsBlood,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    10,
                    new SpellDefinition
                    (
                        "Mass Sleep",
                        687,
                        0x5DC9,
                        "Vas Zu",
                        14,
                        45,
                        TargetType.Harmful,
                        Reagents.Ginseng,
                        Reagents.Nightshade,
                        Reagents.SpidersSilk
                    )
                },
                {
                    11,
                    new SpellDefinition
                    (
                        "Cleansing Winds",
                        688,
                        0x5DCA,
                        "In Vas Mani Hur",
                        20,
                        58,
                        TargetType.Beneficial,
                        Reagents.DragonsBlood,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    12,
                    new SpellDefinition
                    (
                        "Bombard",
                        689,
                        0x5DCB,
                        "Corp Por Ylem",
                        20,
                        58,
                        TargetType.Harmful,
                        Reagents.Bloodmoss,
                        Reagents.DragonsBlood,
                        Reagents.Garlic,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    13,
                    new SpellDefinition
                    (
                        "Spell Plague",
                        690,
                        0x5DCC,
                        "Vas Rel Jux Ort",
                        40,
                        70,
                        TargetType.Harmful,
                        Reagents.DemonBone,
                        Reagents.DragonsBlood,
                        Reagents.Nightshade,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    14,
                    new SpellDefinition
                    (
                        "Hail Storm",
                        691,
                        0x5DCD,
                        "Kal Des Ylem",
                        50,
                        70,
                        TargetType.Harmful,
                        Reagents.DragonsBlood,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    15,
                    new SpellDefinition
                    (
                        "Nether Cyclone",
                        692,
                        0x5DCE,
                        "Grav Hur",
                        50,
                        83,
                        TargetType.Harmful,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade,
                        Reagents.SulfurousAsh,
                        Reagents.Bloodmoss
                    )
                },
                {
                    16,
                    new SpellDefinition
                    (
                        "Rising Colossus",
                        693,
                        0x5DCF,
                        "Kal Vas Xen Corp Ylem",
                        50,
                        83,
                        TargetType.Neutral,
                        Reagents.DemonBone,
                        Reagents.DragonsBlood,
                        Reagents.FertileDirt,
                        Reagents.Nightshade
                    )
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Mysticism.ToString();

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