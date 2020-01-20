#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Runtime.CompilerServices;

using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class SpellsMastery
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

      
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
                    8, new SpellDefinition("Ethereal Burst", 708, 0x9B8C, 0x9B8C, "Uus Ort Grav", 0, 90, 0, TargetType.Beneficial, Reagents.Bloodmoss, Reagents.Ginseng, Reagents.MandrakeRoot)
                },
                {
                    9, new SpellDefinition("Nether Blast", 709, 0x9B8D, 0x9B8D, "In Vas Xen Por", 40, 90, 0, TargetType.Harmful, Reagents.DragonsBlood, Reagents.DemonBone)
                },
                {
                    10, new SpellDefinition("Mystic Weapon", 710, 0x9B8E, 0x9B8E, "Vas Ylem Wis", 40, 90, 0, TargetType.Neutral, Reagents.FertileDirt, Reagents.Bone)
                },
                {
                    11, new SpellDefinition("Command Undead", 711, 0x9B8F, 0x9B8F, "In Corp Xen Por", 40, 90, 0, TargetType.Neutral, Reagents.DaemonBlood, Reagents.PigIron, Reagents.BatWing)
                },
                {
                    12, new SpellDefinition("Conduit", 712, 0x9B90, 0x9B90, "Uus Corp Grav", 40, 90, 0, TargetType.Harmful, Reagents.NoxCrystal, Reagents.BatWing, Reagents.GraveDust)
                },
                {
                    13, new SpellDefinition("Mana Shield", 713, 0x9B91, 0x9B91, "Faerkulggen", 40, 90, 0, TargetType.Beneficial)
                },
                {
                    14, new SpellDefinition("Summon Reaper", 714, 0x9B92, 0x9B92, "Lartarisstree", 50, 90, 0, TargetType.Neutral)
                },
                {
                    15, new SpellDefinition("Enchanted Summoning", 715, 0x9B93, 0x9B93, "", 0, 90, 0, TargetType.Neutral)
                },
                {
                    16, new SpellDefinition("Anticipate Hit", 716, 0x9B94, 0x9B94, "", 10, 90, 0, TargetType.Neutral)
                },
                {
                    17, new SpellDefinition("Warcry", 717, 0x9B95, 0x9B95, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    18, new SpellDefinition("Intuition", 718, 0x9B96, 0x9B96, "", 0, 90, 0, TargetType.Neutral)
                },
                {
                    19, new SpellDefinition("Rejuvenate", 719, 0x9B97, 0x9B97, "", 10, 90, 35, TargetType.Neutral)
                },
                {
                    20, new SpellDefinition("Holy Fist", 720, 0x9B98, 0x9B98, "", 50, 90, 35, TargetType.Neutral)
                },
                {
                    21, new SpellDefinition("Shadow", 721, 0x9B99, 0x9B99, "", 10, 90, 4, TargetType.Neutral)
                },
                {
                    22, new SpellDefinition("White Tiger Form", 722, 0x9B9A, 0x9B9A, "", 10, 90, 0, TargetType.Neutral)
                },
                {
                    23, new SpellDefinition("Flaming Shot", 723, 0x9B9B, 0x9B9B, "", 30, 90, 0, TargetType.Neutral)
                },
                {
                    24, new SpellDefinition("Playing The Odds", 724, 0x9B9C, 0x9B9C, "", 25, 90, 0, TargetType.Neutral)
                },
                {
                    25, new SpellDefinition("Thrust", 725, 0x9B9D, 0x9B9D, "", 30, 90, 20, TargetType.Neutral)
                },
                {
                    26, new SpellDefinition("Pierce", 726, 0x9B9E, 0x9B9E, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    27, new SpellDefinition("Stagger", 727, 0x9B9F, 0x9B9F, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    28, new SpellDefinition("Toughness", 728, 0x9BA0, 0x9BA0, "", 20, 90, 20, TargetType.Neutral)
                },
                {
                    29, new SpellDefinition("Onslaught", 729, 0x9BA1, 0x9BA1, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    30, new SpellDefinition("Focused Eye", 730, 0x9BA2, 0x9BA2, "", 20, 90, 20, TargetType.Neutral)
                },
                {
                    31, new SpellDefinition("Elemental Fury", 731, 0x9BA3, 0x9BA3, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    32, new SpellDefinition("Called Shot", 732, 0x9BA4, 0x9BA4, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    33, new SpellDefinition("Warrior's Gifts", 733, 0x9BA5, 0x9BA5, "", 50, 90, 0, TargetType.Neutral)
                },
                {
                    34, new SpellDefinition("Shield Bash", 734, 0x9BA6, 0x9BA6, "", 50, 90, 0, TargetType.Neutral)
                },
                {
                    35, new SpellDefinition("Bodyguard", 735, 0x9BA7, 0x9BA7, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    36, new SpellDefinition("Heighten Senses", 736, 0x9BA8, 0x9BA8, "", 10, 90, 10, TargetType.Neutral)
                },
                {
                    37, new SpellDefinition("Tolerance", 737, 0x9BA9, 0x9BA9, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    38, new SpellDefinition("Injected Strike", 738, 0x9BAA, 0x9BAA, "", 30, 90, 0, TargetType.Neutral)
                },
                {
                    39, new SpellDefinition("Potency", 739, 0x9BAB, 0x9BAB, "", 0, 90, 0, TargetType.Neutral)
                },
                {
                    40, new SpellDefinition("Rampage", 740, 0x9BAC, 0x9BAC, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    41, new SpellDefinition("Fists of Fury", 741, 0x9BAD, 0x9BAD, "", 20, 90, 0, TargetType.Neutral)
                },
                {
                    42, new SpellDefinition("Knockout", 742, 0x9BAE, 0x9BAE, "", 0, 90, 0, TargetType.Neutral)
                },
                {
                    43, new SpellDefinition("Whispering", 743, 0x9BAF, 0x9BAF, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    44, new SpellDefinition("Combat Training", 744, 0x9BB0, 0x9BB0, "", 40, 90, 0, TargetType.Neutral)
                },
                {
                    45, new SpellDefinition("Boarding", 745, 0x9BB1, 0x9BB1, "", 0, 90, 0, TargetType.Neutral)
                },

            };
        }


        public static readonly int[][] SpellbookIndices =
        {
            new [] { 1,  2,  3,  4,  5,  6,  7,  8 },
            new [] { 9,  10, 11, 12, 13, 14, 19, 20,  },
            new [] { 17, 21, 22, 25, 34, 35, 36,      },
            new [] { 27, 28, 29, 32, 37, 38, 40, 41,  },
            new [] { 23, 24, 30, 31, 43, 44,          },
            new [] { 15, 16, 18, 33, 39, 42, 45 }
        };

        public static readonly string SpellBookName = SpellBookType.Mastery.ToString();
        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;
        
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

        public static List<int> GetSpellListByGroupName(string group)
        {
            List<int> list = new List<int>();

            switch (group.ToLower())
            {
                default:
                case "provocation":
                    list.Add(1);
                    list.Add(2);
                    break;
                case "peacemaking":
                    list.Add(3);
                    list.Add(4);
                    break;
                case "discordance":
                    list.Add(5);
                    list.Add(6);
                    break;
                case "magery":
                    list.Add(15);
                    list.Add(7);
                    list.Add(8);
                    break;
                case "mysticism":
                    list.Add(15);
                    list.Add(9);
                    list.Add(10);
                    break;
                case "necromancy":
                    list.Add(15);
                    list.Add(11);
                    list.Add(12);
                    break;
                case "spellweaving":
                    list.Add(15);
                    list.Add(13);
                    list.Add(14);
                    break;
                case "bushido":
                    list.Add(18);
                    list.Add(16);
                    list.Add(17);
                    break;
                case "chivalry":
                    list.Add(18);
                    list.Add(19);
                    list.Add(20);
                    break;
                case "ninjitsu":
                    list.Add(18);
                    list.Add(21);
                    list.Add(22);
                    break;
                case "archery":
                    list.Add(33);
                    list.Add(23);
                    list.Add(24);
                    break;
                case "fencing":
                    list.Add(33);
                    list.Add(25);
                    list.Add(26);
                    break;
                case "mace fighting":
                    list.Add(33);
                    list.Add(27);
                    list.Add(28);
                    break;
                case "swordsmanship":
                    list.Add(33);
                    list.Add(29);
                    list.Add(30);
                    break;
                case "throwing":
                    list.Add(33);
                    list.Add(31);
                    list.Add(32);
                    break;
                case "parrying":
                    list.Add(34);
                    list.Add(35);
                    list.Add(36);
                    break;
                case "poisoning":
                    list.Add(39);
                    list.Add(37);
                    list.Add(38);
                    break;
                case "wrestling":
                    list.Add(40);
                    list.Add(42);
                    list.Add(41);
                    break;
                case "animal taming":
                    list.Add(45);
                    list.Add(43);
                    list.Add(44);
                    break;
            }

            return list;
        }

        public static string GetMasteryGroupByID(int id)
        {
            switch (id)
            {
                default:
                case 1:
                case 2:
                    return "Provocation";
                case 3:
                case 4:
                    return "Peacemaking";
                case 5:
                case 6:
                    return "Discordance";
                case 7:
                case 8:
                    return "Magery";
                case 9:
                case 10:
                    return "Mysticism";
                case 11:
                case 12:
                    return "Necromancy";
                case 13:
                case 14:
                    return "Spellweaving";
                case 16:
                case 17:
                    return "Bushido";
                case 19:
                case 20:
                    return "Chivalry";
                case 21:
                case 22:
                    return "Ninjitsu";
                case 23:
                case 24:
                    return "Archery";
                case 25:
                case 26:
                    return "Fencing";
                case 27:
                case 28:
                    return "Mace Fighting";
                case 29:
                case 30:
                    return "Swordmanship";
                case 31:
                case 32:
                    return "Throwing";
                case 34:
                case 35:
                case 36:
                    return "Parrying";
                case 37:
                case 38:
                case 39:
                    return "Poisoning";
                case 40:
                case 41:
                case 42:
                    return "Wrestling";
                case 43:
                case 44:
                case 45:
                    return "Animal Taming";
                case 15:
                case 18:
                case 33:
                    return "Passive";
            }
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