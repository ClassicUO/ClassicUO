#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Data
{
    internal static class SpellsMagery
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        static SpellsMagery()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // first circle
                {
                    1, new SpellDefinition("Clumsy", 1, 0x1B58, Reagents.Bloodmoss, Reagents.Nightshade)
                },
                {
                    2, new SpellDefinition("Create Food", 2, 0x1B59, Reagents.Garlic, Reagents.Ginseng, Reagents.MandrakeRoot)
                },
                {
                    3, new SpellDefinition("Feeblemind", 3, 0x1B5A, Reagents.Nightshade, Reagents.Ginseng)
                },
                {
                    4, new SpellDefinition("Heal", 4, 0x1B5B, Reagents.Garlic, Reagents.Ginseng, Reagents.SpidersSilk)
                },
                {
                    5, new SpellDefinition("Magic Arrow", 5, 0x1B5C, Reagents.SulfurousAsh)
                },
                {
                    6, new SpellDefinition("Night Sight", 6, 0x1B5D, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    7, new SpellDefinition("Reactive Armor", 7, 0x1B5E, Reagents.Garlic, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    8, new SpellDefinition("Weaken", 8, 0x1B5F, Reagents.Garlic, Reagents.Nightshade)
                },
                // second circle
                {
                    9, new SpellDefinition("Agility", 9, 0x1B60, Reagents.Bloodmoss, Reagents.MandrakeRoot)
                },
                {
                    10, new SpellDefinition("Cunning", 10, 0x1B61, Reagents.Nightshade, Reagents.MandrakeRoot)
                },
                {
                    11, new SpellDefinition("Cure", 11, 0x1B62, Reagents.Garlic, Reagents.Ginseng)
                },
                {
                    12, new SpellDefinition("Harm", 12, 0x1B63, Reagents.Nightshade, Reagents.SpidersSilk)
                },
                {
                    13, new SpellDefinition("Magic Trap", 13, 0x1B64, Reagents.Garlic, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    14, new SpellDefinition("Magic Untrap", 14, 0x1B65, Reagents.Bloodmoss, Reagents.SulfurousAsh)
                },
                {
                    15, new SpellDefinition("Protection", 15, 0x1B66, Reagents.Garlic, Reagents.Ginseng, Reagents.SulfurousAsh)
                },
                {
                    16, new SpellDefinition("Strength", 16, 0x1B67, Reagents.MandrakeRoot, Reagents.Nightshade)
                },
                // third circle
                {
                    17, new SpellDefinition("Bless", 17, 0x1B68, Reagents.Garlic, Reagents.MandrakeRoot)
                },
                {
                    18, new SpellDefinition("Fireball", 18, 0x1B69, Reagents.BlackPearl)
                },
                {
                    19, new SpellDefinition("Magic Lock", 19, 0x1B6a, Reagents.Bloodmoss, Reagents.Garlic, Reagents.SulfurousAsh)
                },
                {
                    20, new SpellDefinition("Poison", 20, 0x1B6b, Reagents.Nightshade)
                },
                {
                    21, new SpellDefinition("Telekinesis", 21, 0x1B6c, Reagents.Bloodmoss, Reagents.MandrakeRoot)
                },
                {
                    22, new SpellDefinition("Teleport", 22, 0x1B6d, Reagents.Bloodmoss, Reagents.MandrakeRoot)
                },
                {
                    23, new SpellDefinition("Unlock", 23, 0x1B6e, Reagents.Bloodmoss, Reagents.SulfurousAsh)
                },
                {
                    24, new SpellDefinition("Wall of Stone", 24, 0x1B6f, Reagents.Bloodmoss, Reagents.Garlic)
                },
                // fourth circle
                {
                    25, new SpellDefinition("Arch Cure", 25, 0x1B70, Reagents.Garlic, Reagents.Ginseng, Reagents.MandrakeRoot)
                },
                {
                    26, new SpellDefinition("Arch Protection", 26, 0x1B71, Reagents.Garlic, Reagents.Ginseng, Reagents.MandrakeRoot, Reagents.SulfurousAsh)
                },
                {
                    27, new SpellDefinition("Curse", 27, 0x1B72, Reagents.Garlic, Reagents.Nightshade, Reagents.SulfurousAsh)
                },
                {
                    28, new SpellDefinition("Fire Field", 28, 0x1B73, Reagents.BlackPearl, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    29, new SpellDefinition("Greater Heal", 29, 0x1B74, Reagents.Garlic, Reagents.Ginseng, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                },
                {
                    30, new SpellDefinition("Lightning", 30, 0x1B75, Reagents.MandrakeRoot, Reagents.SulfurousAsh)
                },
                {
                    31, new SpellDefinition("Mana Drain", 31, 0x1B76, Reagents.BlackPearl, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                },
                {
                    32, new SpellDefinition("Recall", 32, 0x1B77, Reagents.BlackPearl, Reagents.Bloodmoss, Reagents.MandrakeRoot)
                },
                // fifth circle
                {
                    33, new SpellDefinition("Blade Spirits", 33, 0x1B78, Reagents.BlackPearl, Reagents.MandrakeRoot, Reagents.Nightshade)
                },
                {
                    34, new SpellDefinition("Dispel Field", 34, 0x1B79, Reagents.BlackPearl, Reagents.Garlic, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    35, new SpellDefinition("Incognito", 35, 0x1B7a, Reagents.Bloodmoss, Reagents.Garlic, Reagents.Nightshade)
                },
                {
                    36, new SpellDefinition("Magic Reflection", 36, 0x1B7b, Reagents.Garlic, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                },
                {
                    37, new SpellDefinition("Mind Blast", 37, 0x1B7c, Reagents.BlackPearl, Reagents.MandrakeRoot, Reagents.Nightshade, Reagents.SulfurousAsh)
                },
                {
                    38, new SpellDefinition("Paralyze", 38, 0x1B7d, Reagents.Garlic, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                },
                {
                    39, new SpellDefinition("Poison Field", 39, 0x1B7e, Reagents.BlackPearl, Reagents.Nightshade, Reagents.SpidersSilk)
                },
                {
                    40, new SpellDefinition("Summon Creature", 40, 0x1B7f, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                },
                // sixth circle
                {
                    41, new SpellDefinition("Dispel", 41, 0x1B80, Reagents.Garlic, Reagents.MandrakeRoot, Reagents.SulfurousAsh)
                },
                {
                    42, new SpellDefinition("Energy Bolt", 42, 0x1B81, Reagents.BlackPearl, Reagents.Nightshade)
                },
                {
                    43, new SpellDefinition("Explosion", 43, 0x1B82, Reagents.Bloodmoss, Reagents.MandrakeRoot)
                },
                {
                    44, new SpellDefinition("Invisibility", 44, 0x1B83, Reagents.Bloodmoss, Reagents.Nightshade)
                },
                {
                    45, new SpellDefinition("Mark", 45, 0x1B84, Reagents.BlackPearl, Reagents.Bloodmoss, Reagents.MandrakeRoot)
                },
                {
                    46, new SpellDefinition("Mass Curse", 46, 0x1B85, Reagents.Garlic, Reagents.MandrakeRoot, Reagents.Nightshade, Reagents.SulfurousAsh)
                },
                {
                    47, new SpellDefinition("Paralyze Field", 47, 0x1B86, Reagents.BlackPearl, Reagents.Ginseng, Reagents.SpidersSilk)
                },
                {
                    48, new SpellDefinition("Reveal", 48, 0x1B87, Reagents.Bloodmoss, Reagents.SulfurousAsh)
                },
                // seventh circle
                {
                    49, new SpellDefinition("Chain Lightning", 49, 0x1B88, Reagents.BlackPearl, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SulfurousAsh)
                },
                {
                    50, new SpellDefinition("Energy Field", 50, 0x1B89, Reagents.BlackPearl, Reagents.MandrakeRoot, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    51, new SpellDefinition("Flamestrike", 51, 0x1B8a, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    52, new SpellDefinition("Gate Travel", 52, 0x1B8b, Reagents.BlackPearl, Reagents.MandrakeRoot, Reagents.SulfurousAsh)
                },
                {
                    53, new SpellDefinition("Mana Vampire", 53, 0x1B8c, Reagents.BlackPearl, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                },
                {
                    54, new SpellDefinition("Mass Dispel", 54, 0x1B8d, Reagents.BlackPearl, Reagents.Garlic, Reagents.MandrakeRoot, Reagents.SulfurousAsh)
                },
                {
                    55, new SpellDefinition("Meteor Swarm", 55, 0x1B8e, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    56, new SpellDefinition("Polymorph", 56, 0x1B8f, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                },
                // eighth circle
                {
                    57, new SpellDefinition("Earthquake", 57, 0x1B90, Reagents.Bloodmoss, Reagents.Ginseng, Reagents.MandrakeRoot, Reagents.SulfurousAsh)
                },
                {
                    58, new SpellDefinition("Energy Vortex", 58, 0x1B91, Reagents.BlackPearl, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.Nightshade)
                },
                {
                    59, new SpellDefinition("Resurrection", 59, 0x1B92, Reagents.Bloodmoss, Reagents.Ginseng, Reagents.Garlic)
                },
                {
                    60, new SpellDefinition("Air Elemental", 60, 0x1B93, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                },
                {
                    61, new SpellDefinition("Summon Daemon", 61, 0x1B94, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    62, new SpellDefinition("Earth Elemental", 62, 0x1B95, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                },
                {
                    63, new SpellDefinition("Fire Elemental", 63, 0x1B96, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SpidersSilk, Reagents.SulfurousAsh)
                },
                {
                    64, new SpellDefinition("Water Elemental", 64, 0x1B97, Reagents.Bloodmoss, Reagents.MandrakeRoot, Reagents.SpidersSilk)
                }
            };
        }

        public static string[] CircleNames { get; } =
        {
            "First Circle", "Second Circle", "Third Circle", "Fourth Circle", "Fifth Circle", "Sixth Circle", "Seventh Circle", "Eighth Circle"
        };

        public static string[][] SpecialReagentsChars { get; internal set; } =
        {
            new[]
            {
                "Clumsy", "U J"
            },
            new[]
            {
                "Create Food", "I M Y"
            },
            new[]
            {
                "Feeblemind", "R W"
            },
            new[]
            {
                "Heal", "I M"
            },
            new[]
            {
                "Magic Arrow", "I P Y"
            },
            new[]
            {
                "Night Sight", "I L"
            },
            new[]
            {
                "Reactive Armor", "F S"
            },
            new[]
            {
                "Weaken", "D M"
            },
            new[]
            {
                "Agility", "E U"
            },
            new[]
            {
                "Cunning", "U W"
            },
            new[]
            {
                "Cure", "A N"
            },
            new[]
            {
                "Harm", "A M"
            },
            new[]
            {
                "Magic Trap", "I J"
            },
            new[]
            {
                "Magic Untrap", "A J"
            },
            new[]
            {
                "Protection", "U S"
            },
            new[]
            {
                "Strength", "U M"
            },
            new[]
            {
                "Bless", "R S"
            },
            new[]
            {
                "Fireball", "V F"
            },
            new[]
            {
                "Magic Lock", "A P"
            },
            new[]
            {
                "Poison", "I N"
            },
            new[]
            {
                "Telekinesis", "O P Y"
            },
            new[]
            {
                "Teleport", "R P"
            },
            new[]
            {
                "Unlock", "E P"
            },
            new[]
            {
                "Wall of Stone", "I S Y"
            },
            new[]
            {
                "Arch Cure", "V A N"
            },
            new[]
            {
                "Arch Protection", "V U S"
            },
            new[]
            {
                "Curse", "D S"
            },
            new[]
            {
                "Fire Field", "I F G"
            },
            new[]
            {
                "Greater Heal", "I V M"
            },
            new[]
            {
                "Lightning", "P O G"
            },
            new[]
            {
                "Mana Drain", "O R"
            },
            new[]
            {
                "Recall", "K O P"
            },
            new[]
            {
                "Blade Spirits", "I H J Y"
            },
            new[]
            {
                "Dispel Field", "A G"
            },
            new[]
            {
                "Incognito", "K I E"
            },
            new[]
            {
                "Magic Reflection", "I J S"
            },
            new[]
            {
                "Mind Blast", "P C W"
            },
            new[]
            {
                "Paralyze", "A E P"
            },
            new[]
            {
                "Poison Field", "I N G"
            },
            new[]
            {
                "Summ. Creature", "K X"
            },
            new[]
            {
                "Dispel", "A O"
            },
            new[]
            {
                "Energy Bolt", "C P"
            },
            new[]
            {
                "Explosion", "V O F"
            },
            new[]
            {
                "Invisibility", "A L X"
            },
            new[]
            {
                "Mark", "K P Y"
            },
            new[]
            {
                "Mass Curse", "V D S"
            },
            new[]
            {
                "Paralyze Field", "I E G"
            },
            new[]
            {
                "Reveal", "W Q"
            },
            new[]
            {
                "Chain Lightning", "V O G"
            },
            new[]
            {
                "Energy Field", "I S G"
            },
            new[]
            {
                "Flame Strike", "K V F"
            },
            new[]
            {
                "Gate Travel", "V R P"
            },
            new[]
            {
                "Mana Vampire", "O S"
            },
            new[]
            {
                "Mass Dispel", "V A O"
            },
            new[]
            {
                "Meteor Swarm", "F K D Y"
            },
            new[]
            {
                "Polymorph", "V Y R"
            },
            new[]
            {
                "Earthquake", "I V P"
            },
            new[]
            {
                "Energy Vortex", "V C P"
            },
            new[]
            {
                "Resurrection", "A C"
            },
            new[]
            {
                "Air Elemental", "K V X H"
            },
            new[]
            {
                "Summon Daemon", "K V X C"
            },
            new[]
            {
                "Earth Elemental", "K V X Y"
            },
            new[]
            {
                "Fire Elemental", "K V X F"
            },
            new[]
            {
                "Water Elemental", "K V X A"
            }
        };

        public static SpellDefinition GetSpell(int index)
        {
            return _spellsDict.TryGetValue(index, out SpellDefinition spell) ? spell : SpellDefinition.EmptySpell;
        }
    }
}