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
using System.Runtime.CompilerServices;

using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class SpellsMastery
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        public readonly struct MasteryEntry
        {
            public MasteryEntry(int a, int b, int p)
            {
                SpellA = a;
                SpellB = b;
                Passive = p;
            }

            public readonly int SpellA, SpellB, Passive;
        }

        static SpellsMastery()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
               {
                    1, new SpellDefinition("Inspire", 701, 0x945, 0x945, "Uus Por", 16, 90, 4, TargetType.Beneficial, Reagents.None)
                },
                {
                    2, new SpellDefinition("Invigorate", 702, 0x946, 0x946, "An Zu", 22, 90, 5, TargetType.Beneficial, Reagents.None)
                },
                {
                    3, new SpellDefinition("Resilience", 703, 0x947, 0x947, "Kal Mani Tym", 16, 90, 4, TargetType.Beneficial, Reagents.None)
                },
                {
                    4, new SpellDefinition("Perseverance", 704, 0x948, 0x948, "Uus Jux Sanct", 18, 90, 5, TargetType.Beneficial, Reagents.None)
                },
                {
                    5, new SpellDefinition("Tribulation", 705, 0x949, 0x949, "In Jux Hur Rel", 24, 90, 10, TargetType.Harmful, Reagents.None)
                },
                {
                    6, new SpellDefinition("Despair", 706, 0x94A, 0x94A, "Kal Des Mani Tym", 26, 90, 12, TargetType.Harmful, Reagents.None)
                },
                {
                    7, new SpellDefinition("Death Ray", 707, 0x9B8B, 0x9B8B, "In Grav Corp", 50, 90, 35, TargetType.Harmful, Reagents.BlackPearl, Reagents.Bloodmoss, Reagents.SpidersSilk)
                },
                {
                    8, new SpellDefinition("Ethereal Burst", 709, 0x9B8C, 0x9B8C, "Uus Ort Grav", 0, 90, 0, TargetType.Beneficial, Reagents.Bloodmoss, Reagents.Ginseng, Reagents.MandrakeRoot)
                },
                {
                    9, new SpellDefinition("Nether Blast", 710, 0x9B8D, 0x9B8D, "In Vas Xen Por", 40, 90, 0, TargetType.Harmful, Reagents.DragonsBlood, Reagents.DemonBone)
                },
                {
                    10, new SpellDefinition("Mystic Weapon", 711, 0x9B8E, 0x9B8E, "Vas Ylem Wis", 40, 90, 0, TargetType.Neutral, Reagents.FertileDirt, Reagents.Bone)
                },
                {
                    11, new SpellDefinition("Command Undead", 712, 0x9B8F, 0x9B8F, "In Corp Xen Por", 40, 90, 0, TargetType.Neutral, Reagents.DaemonBlood, Reagents.PigIron, Reagents.BatWing)
                },
                {
                    12, new SpellDefinition("Conduit", 713, 0x9B90, 0x9B90, "Uus Corp Grav", 40, 90, 0, TargetType.Harmful, Reagents.NoxCrystal, Reagents.BatWing, Reagents.GraveDust)
                },
                {
                    13, new SpellDefinition("Mana Shield", 716, 0x9B91, 0x9B91, "Faerkulggen", 40, 90, 0, TargetType.Beneficial)
                },
                {
                    14, new SpellDefinition("Summon Reaper", 718, 0x9B92, 0x9B92, "Lartarisstree", 50, 90, 0, TargetType.Neutral)
                },
                {
                    15, new SpellDefinition("Enchanted Summoning", 714, 0x9B93, 0x9B93, "", 0, 90, 0, TargetType.Neutral)
                },
                {
                    16, new SpellDefinition("Anticipate Hit", 715, 0x9B94, 0x9B94, "", 10, 90, 0, TargetType.Neutral)
                },
                {
                    17, new SpellDefinition("Warcry", 721, 0x9B95, 0x9B95, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    18, new SpellDefinition("Intuition", 717, 0x9B96, 0x9B96, "", 0, 90, 0, TargetType.Neutral)
                },
                {
                    19, new SpellDefinition("Rejuvenate", 723, 0x9B97, 0x9B97, "", 10, 90, 35, TargetType.Neutral)
                },
                {
                    20, new SpellDefinition("Holy Fist", 724, 0x9B98, 0x9B98, "", 50, 90, 35, TargetType.Neutral)
                },
                {
                    21, new SpellDefinition("Shadow", 725, 0x9B99, 0x9B99, "", 10, 90, 4, TargetType.Neutral)
                },
                {
                    22, new SpellDefinition("White Tiger Form", 726, 0x9B9A, 0x9B9A, "", 10, 90, 0, TargetType.Neutral)
                },
                {
                    23, new SpellDefinition("Flaming Shot", 727, 0x9B9B, 0x9B9B, "", 30, 90, 0, TargetType.Neutral)
                },
                {
                    24, new SpellDefinition("Playing The Odds", 728, 0x9B9C, 0x9B9C, "", 25, 90, 0, TargetType.Neutral)
                },
                {
                    25, new SpellDefinition("Thrust", 729, 0x9B9D, 0x9B9D, "", 30, 90, 20, TargetType.Neutral)
                },
                {
                    26, new SpellDefinition("Pierce", 730, 0x9B9E, 0x9B9E, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    27, new SpellDefinition("Stagger", 731, 0x9B9F, 0x9B9F, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    28, new SpellDefinition("Toughness", 733, 0x9BA0, 0x9BA0, "", 20, 90, 20, TargetType.Neutral)
                },
                {
                    29, new SpellDefinition("Onslaught", 734, 0x9BA1, 0x9BA1, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    30, new SpellDefinition("Focused Eye", 735, 0x9BA2, 0x9BA2, "", 20, 90, 20, TargetType.Neutral)
                },
                {
                    31, new SpellDefinition("Elemental Fury", 736, 0x9BA3, 0x9BA3, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    32, new SpellDefinition("Called Shot", 737, 0x9BA4, 0x9BA4, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    33, new SpellDefinition("Warrior's Gifts", 732, 0x9BA5, 0x9BA5, "", 50, 90, 0, TargetType.Neutral)
                },
                {
                    34, new SpellDefinition("Shield Bash", 740, 0x9BA6, 0x9BA6, "", 50, 90, 0, TargetType.Neutral)
                },
                {
                    35, new SpellDefinition("Bodyguard", 742, 0x9BA7, 0x9BA7, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    36, new SpellDefinition("Heighten Senses", 743, 0x9BA8, 0x9BA8, "", 10, 90, 10, TargetType.Neutral)
                },
                {
                    37, new SpellDefinition("Tolerance", 743, 0x9BA9, 0x9BA9, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    38, new SpellDefinition("Injected Strike", 743, 0x9BAA, 0x9BAA, "", 30, 90, 0, TargetType.Neutral)
                },
                {
                    39, new SpellDefinition("Potency", 738, 0x9BAB, 0x9BAB, "", 0, 90, 0, TargetType.Neutral)
                },
                {
                    40, new SpellDefinition("Rampage", 743, 0x9BAC, 0x9BAC, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    41, new SpellDefinition("Fists of Fury", 743, 0x9BAD, 0x9BAD, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    42, new SpellDefinition("Knockout", 741, 0x9BAE, 0x9BAE, "", 0, 90, 0, TargetType.Neutral)
                },
                {
                    43, new SpellDefinition("Whispering", 743, 0x9BAF, 0x9BAF, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    44, new SpellDefinition("Combat Training", 743, 0x9BB0, 0x9BB0, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    45, new SpellDefinition("Boarding", 744, 0x9BB1, 0x9BB1, "", 0, 90, 0, TargetType.Neutral)
                },

            };
        }

        public static readonly Dictionary<long, MasteryEntry> ActiveMasteryIndex = new Dictionary<long, MasteryEntry>()
        {
            { 1151945, new MasteryEntry(705, 706, 0) },
            { 1151946, new MasteryEntry(701, 702, 0) },
            { 1151947, new MasteryEntry(703, 704, 0) },
            { 1155771, new MasteryEntry(707, 708, 715) },
            { 1155772, new MasteryEntry(709, 710, 715) },
            { 1155773, new MasteryEntry(711, 712, 715) },
            { 1155774, new MasteryEntry(713, 714, 715) },
            { 1155775, new MasteryEntry(716, 717, 718) },
            { 1155776, new MasteryEntry(719, 720, 718) },
            { 1155777, new MasteryEntry(721, 722, 718) },
            { 1155778, new MasteryEntry(725, 726, 733) },
            { 1155779, new MasteryEntry(727, 728, 733) },
            { 1155780, new MasteryEntry(729, 730, 733) },
            { 1155781, new MasteryEntry(731, 732, 718) },
            { 1155782, new MasteryEntry(734, 735, 736) },
            { 1155783, new MasteryEntry(737, 738, 739) },
            { 1155784, new MasteryEntry(740, 741, 742) },
            { 1155785, new MasteryEntry(743, 744, 745) },
            { 1155786, new MasteryEntry(723, 724, 733) },
        };

        public static readonly string SpellBookName = SpellBookType.Mastery.ToString();

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;

        public static bool IsPassive(int index)
        {
            return index == 714 || index == 715 || index == 717 ||
                   index == 732 || index == 738 || index == 741 ||
                   index == 744;
        }

        public static readonly int[] Passives = {714, 715, 717, 732, 738, 741, 744};

        internal static string GetUsedSkillName(int spellid)
        {
            int div = (MaxSpellCount * 3) >> 3;

            if (div <= 0)
                div = 1;
            int group = spellid / div;


            switch (group)
            {
                case 0:
                    return "Provocation";
                case 1:
                    return "Peacemaking";
                case 3:
                    return "Discordance";
                case 4:
                    return "Magery";
                case 5:
                    return "Mysticism";
                case 6:
                    return "Necromancy";
                case 7:
                    return "Spellweaving";
                case 8:
                    return "Passive";
                case 9:
                    return "Bushido";

            }

            if (group == 0)
                return "Provocation";

            if (group == 1)
                return "Peacemaking";

            return "Discordance";
        }

        public static SpellDefinition GetSpell(int spellIndex)
        {
            if (_spellsDict.TryGetValue(spellIndex, out SpellDefinition spell))
                return spell;

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