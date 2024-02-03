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
using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;

namespace ClassicUO.Game.Data
{
    internal static class SpellsMagery
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        private static string[] _spRegsChars;

        static SpellsMagery()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // first circle
                {
                    1,
                    new SpellDefinition
                    (
                        "Clumsy",
                        1,
                        0x1B58,
                        "Uus Jux",
                        TargetType.Harmful,
                        Reagents.Bloodmoss,
                        Reagents.Nightshade
                    )
                },
                {
                    2,
                    new SpellDefinition
                    (
                        "Create Food",
                        2,
                        0x1B59,
                        "In Mani Ylem",
                        TargetType.Neutral,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    3,
                    new SpellDefinition
                    (
                        "Feeblemind",
                        3,
                        0x1B5A,
                        "Rel Wis",
                        TargetType.Harmful,
                        Reagents.Nightshade,
                        Reagents.Ginseng
                    )
                },
                {
                    4,
                    new SpellDefinition
                    (
                        "Heal",
                        4,
                        0x1B5B,
                        "In Mani",
                        TargetType.Beneficial,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.SpidersSilk
                    )
                },
                {
                    5,
                    new SpellDefinition
                    (
                        "Magic Arrow",
                        5,
                        0x1B5C,
                        "In Por Ylem",
                        TargetType.Harmful,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    6,
                    new SpellDefinition
                    (
                        "Night Sight",
                        6,
                        0x1B5D,
                        "In Lor",
                        TargetType.Beneficial,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    7,
                    new SpellDefinition
                    (
                        "Reactive Armor",
                        7,
                        0x1B5E,
                        "Flam Sanct",
                        TargetType.Beneficial,
                        Reagents.Garlic,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    8,
                    new SpellDefinition
                    (
                        "Weaken",
                        8,
                        0x1B5F,
                        "Des Mani",
                        TargetType.Harmful,
                        Reagents.Garlic,
                        Reagents.Nightshade
                    )
                },
                // second circle
                {
                    9,
                    new SpellDefinition
                    (
                        "Agility",
                        9,
                        0x1B60,
                        "Ex Uus",
                        TargetType.Beneficial,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    10,
                    new SpellDefinition
                    (
                        "Cunning",
                        10,
                        0x1B61,
                        "Uus Wis",
                        TargetType.Beneficial,
                        Reagents.Nightshade,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    11,
                    new SpellDefinition
                    (
                        "Cure",
                        11,
                        0x1B62,
                        "An Nox",
                        TargetType.Beneficial,
                        Reagents.Garlic,
                        Reagents.Ginseng
                    )
                },
                {
                    12,
                    new SpellDefinition
                    (
                        "Harm",
                        12,
                        0x1B63,
                        "An Mani",
                        TargetType.Harmful,
                        Reagents.Nightshade,
                        Reagents.SpidersSilk
                    )
                },
                {
                    13,
                    new SpellDefinition
                    (
                        "Magic Trap",
                        13,
                        0x1B64,
                        "In Jux",
                        TargetType.Neutral,
                        Reagents.Garlic,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    14,
                    new SpellDefinition
                    (
                        "Magic Untrap",
                        14,
                        0x1B65,
                        "An Jux",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    15,
                    new SpellDefinition
                    (
                        "Protection",
                        15,
                        0x1B66,
                        "Uus Sanct",
                        TargetType.Beneficial,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    16,
                    new SpellDefinition
                    (
                        "Strength",
                        16,
                        0x1B67,
                        "Uus Mani",
                        TargetType.Beneficial,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade
                    )
                },
                // third circle
                {
                    17,
                    new SpellDefinition
                    (
                        "Bless",
                        17,
                        0x1B68,
                        "Rel Sanct",
                        TargetType.Beneficial,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    18, new SpellDefinition
                    (
                        "Fireball",
                        18,
                        0x1B69,
                        "Vas Flam",
                        TargetType.Harmful,
                        Reagents.BlackPearl
                    )
                },
                {
                    19,
                    new SpellDefinition
                    (
                        "Magic Lock",
                        19,
                        0x1B6a,
                        "An Por",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.Garlic,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    20, new SpellDefinition
                    (
                        "Poison",
                        20,
                        0x1B6b,
                        "In Nox",
                        TargetType.Harmful,
                        Reagents.Nightshade
                    )
                },
                {
                    21,
                    new SpellDefinition
                    (
                        "Telekinesis",
                        21,
                        0x1B6c,
                        "Ort Por Ylem",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    22,
                    new SpellDefinition
                    (
                        "Teleport",
                        22,
                        0x1B6d,
                        "Rel Por",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    23,
                    new SpellDefinition
                    (
                        "Unlock",
                        23,
                        0x1B6e,
                        "Ex Por",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    24,
                    new SpellDefinition
                    (
                        "Wall of Stone",
                        24,
                        0x1B6f,
                        "In Sanct Ylem",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.Garlic
                    )
                },
                // fourth circle
                {
                    25,
                    new SpellDefinition
                    (
                        "Arch Cure",
                        25,
                        0x1B70,
                        "Vas An Nox",
                        TargetType.Beneficial,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    26,
                    new SpellDefinition
                    (
                        "Arch Protection",
                        26,
                        0x1B71,
                        "Vas Uus Sanct",
                        TargetType.Beneficial,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    27,
                    new SpellDefinition
                    (
                        "Curse",
                        27,
                        0x1B72,
                        "Des Sanct",
                        TargetType.Harmful,
                        Reagents.Garlic,
                        Reagents.Nightshade,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    28,
                    new SpellDefinition
                    (
                        "Fire Field",
                        28,
                        0x1B73,
                        "In Flam Grav",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    29,
                    new SpellDefinition
                    (
                        "Greater Heal",
                        29,
                        0x1B74,
                        "In Vas Mani",
                        TargetType.Beneficial,
                        Reagents.Garlic,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    30,
                    new SpellDefinition
                    (
                        "Lightning",
                        30,
                        0x1B75,
                        "Por Ort Grav",
                        TargetType.Harmful,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    31,
                    new SpellDefinition
                    (
                        "Mana Drain",
                        31,
                        0x1B76,
                        "Ort Rel",
                        TargetType.Harmful,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    32,
                    new SpellDefinition
                    (
                        "Recall",
                        32,
                        0x1B77,
                        "Kal Ort Por",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                // fifth circle
                {
                    33,
                    new SpellDefinition
                    (
                        "Blade Spirits",
                        33,
                        0x1B78,
                        "In Jux Hur Ylem",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade
                    )
                },
                {
                    34,
                    new SpellDefinition
                    (
                        "Dispel Field",
                        34,
                        0x1B79,
                        "An Grav",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.Garlic,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    35,
                    new SpellDefinition
                    (
                        "Incognito",
                        35,
                        0x1B7a,
                        "Kal In Ex",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.Garlic,
                        Reagents.Nightshade
                    )
                },
                {
                    36,
                    new SpellDefinition
                    (
                        "Magic Reflection",
                        36,
                        0x1B7b,
                        "In Jux Sanct",
                        TargetType.Beneficial,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    37,
                    new SpellDefinition
                    (
                        "Mind Blast",
                        37,
                        0x1B7c,
                        "Por Corp Wis",
                        TargetType.Harmful,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    38,
                    new SpellDefinition
                    (
                        "Paralyze",
                        38,
                        0x1B7d,
                        "An Ex Por",
                        TargetType.Harmful,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    39,
                    new SpellDefinition
                    (
                        "Poison Field",
                        39,
                        0x1B7e,
                        "In Nox Grav",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.Nightshade,
                        Reagents.SpidersSilk
                    )
                },
                {
                    40,
                    new SpellDefinition
                    (
                        "Summon Creature",
                        40,
                        0x1B7f,
                        "Kal Xen",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                // sixth circle
                {
                    41,
                    new SpellDefinition
                    (
                        "Dispel",
                        41,
                        0x1B80,
                        "An Ort",
                        TargetType.Neutral,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    42,
                    new SpellDefinition
                    (
                        "Energy Bolt",
                        42,
                        0x1B81,
                        "Corp Por",
                        TargetType.Harmful,
                        Reagents.BlackPearl,
                        Reagents.Nightshade
                    )
                },
                {
                    43,
                    new SpellDefinition
                    (
                        "Explosion",
                        43,
                        0x1B82,
                        "Vas Ort Flam",
                        TargetType.Harmful,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    44,
                    new SpellDefinition
                    (
                        "Invisibility",
                        44,
                        0x1B83,
                        "An Lor Xen",
                        TargetType.Beneficial,
                        Reagents.Bloodmoss,
                        Reagents.Nightshade
                    )
                },
                {
                    45,
                    new SpellDefinition
                    (
                        "Mark",
                        45,
                        0x1B84,
                        "Kal Por Ylem",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot
                    )
                },
                {
                    46,
                    new SpellDefinition
                    (
                        "Mass Curse",
                        46,
                        0x1B85,
                        "Vas Des Sanct",
                        TargetType.Harmful,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    47,
                    new SpellDefinition
                    (
                        "Paralyze Field",
                        47,
                        0x1B86,
                        "In Ex Grav",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.Ginseng,
                        Reagents.SpidersSilk
                    )
                },
                {
                    48,
                    new SpellDefinition
                    (
                        "Reveal",
                        48,
                        0x1B87,
                        "Wis Quas",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.SulfurousAsh
                    )
                },
                // seventh circle
                {
                    49,
                    new SpellDefinition
                    (
                        "Chain Lightning",
                        49,
                        0x1B88,
                        "Vas Ort Grav",
                        TargetType.Harmful,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    50,
                    new SpellDefinition
                    (
                        "Energy Field",
                        50,
                        0x1B89,
                        "In Sanct Grav",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    51,
                    new SpellDefinition
                    (
                        "Flamestrike",
                        51,
                        0x1B8a,
                        "Kal Vas Flam",
                        TargetType.Harmful,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    52,
                    new SpellDefinition
                    (
                        "Gate Travel",
                        52,
                        0x1B8b,
                        "Vas Rel Por",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    53,
                    new SpellDefinition
                    (
                        "Mana Vampire",
                        53,
                        0x1B8c,
                        "Ort Sanct",
                        TargetType.Harmful,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    54,
                    new SpellDefinition
                    (
                        "Mass Dispel",
                        54,
                        0x1B8d,
                        "Vas An Ort",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.Garlic,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    55,
                    new SpellDefinition
                    (
                        "Meteor Swarm",
                        55,
                        0x1B8e,
                        "Flam Kal Des Ylem",
                        TargetType.Harmful,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    56,
                    new SpellDefinition
                    (
                        "Polymorph",
                        56,
                        0x1B8f,
                        "Vas Ylem Rel",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                // eighth circle
                {
                    57,
                    new SpellDefinition
                    (
                        "Earthquake",
                        57,
                        0x1B90,
                        "In Vas Por",
                        TargetType.Harmful,
                        Reagents.Bloodmoss,
                        Reagents.Ginseng,
                        Reagents.MandrakeRoot,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    58,
                    new SpellDefinition
                    (
                        "Energy Vortex",
                        58,
                        0x1B91,
                        "Vas Corp Por",
                        TargetType.Neutral,
                        Reagents.BlackPearl,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.Nightshade
                    )
                },
                {
                    59,
                    new SpellDefinition
                    (
                        "Resurrection",
                        59,
                        0x1B92,
                        "An Corp",
                        TargetType.Beneficial,
                        Reagents.Bloodmoss,
                        Reagents.Ginseng,
                        Reagents.Garlic
                    )
                },
                {
                    60,
                    new SpellDefinition
                    (
                        "Air Elemental",
                        60,
                        0x1B93,
                        "Kal Vas Xen Hur",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    61,
                    new SpellDefinition
                    (
                        "Summon Daemon",
                        61,
                        0x1B94,
                        "Kal Vas Xen Corp",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    62,
                    new SpellDefinition
                    (
                        "Earth Elemental",
                        62,
                        0x1B95,
                        "Kal Vas Xen Ylem",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                },
                {
                    63,
                    new SpellDefinition
                    (
                        "Fire Elemental",
                        63,
                        0x1B96,
                        "Kal Vas Xen Flam",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk,
                        Reagents.SulfurousAsh
                    )
                },
                {
                    64,
                    new SpellDefinition
                    (
                        "Water Elemental",
                        64,
                        0x1B97,
                        "Kal Vas Xen An Flam",
                        TargetType.Neutral,
                        Reagents.Bloodmoss,
                        Reagents.MandrakeRoot,
                        Reagents.SpidersSilk
                    )
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Magery.ToString();

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;

        public static string[] CircleNames { get; } =
        {
            "First Circle", "Second Circle", "Third Circle", "Fourth Circle", "Fifth Circle", "Sixth Circle",
            "Seventh Circle", "Eighth Circle"
        };

        public static string[] SpecialReagentsChars
        {
            get
            {
                if (_spRegsChars == null)
                {
                    _spRegsChars = new string[_spellsDict.Max(o => o.Key)];

                    for (int i = _spRegsChars.Length; i > 0; --i)
                    {
                        if (_spellsDict.TryGetValue(i, out SpellDefinition sd))
                        {
                            _spRegsChars[i - 1] = StringHelper.RemoveUpperLowerChars(sd.PowerWords);
                        }
                        else
                        {
                            _spRegsChars[i - 1] = string.Empty;
                        }
                    }
                }

                return _spRegsChars;
            }
        }

        public static SpellDefinition GetSpell(int index)
        {
            return _spellsDict.TryGetValue(index, out SpellDefinition spell) ? spell : SpellDefinition.EmptySpell;
        }

        public static void SetSpell(int id, in SpellDefinition newspell)
        {
            _spRegsChars = null;
            _spellsDict[id] = newspell;
        }

        internal static void Clear()
        {
            _spellsDict.Clear();
        }
    }
}