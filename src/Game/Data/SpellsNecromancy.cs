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

using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class SpellsNecromancy
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        static SpellsNecromancy()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1, new SpellDefinition("Animate Dead", 101, 0x5000, "Uus Corp", 23, 40, TargetType.Neutral, Reagents.DaemonBlood, Reagents.GraveDust)
                },
                {
                    2, new SpellDefinition("Blood Oath", 102, 0x5001, "In Jux Mani Xen", 13, 20, TargetType.Harmful, Reagents.DaemonBlood)
                },
                {
                    3, new SpellDefinition("Corpse Skin", 103, 0x5002, "In Agle Corp Ylem", 11, 20, TargetType.Harmful, Reagents.BatWing, Reagents.GraveDust)
                },
                {
                    4, new SpellDefinition("Curse Weapon", 104, 0x5003, "An Sanct Gra Char", 7, 0, TargetType.Neutral, Reagents.PigIron)
                },
                {
                    5, new SpellDefinition("Evil Omen", 105, 0x5004, "Pas Tym An Sanct", 11, 20, TargetType.Harmful, Reagents.BatWing, Reagents.NoxCrystal)
                },
                {
                    6, new SpellDefinition("Horrific Beast", 106, 0x5005, "Rel Xen Vas Bal", 11, 40, TargetType.Neutral, Reagents.BatWing, Reagents.DaemonBlood)
                },
                {
                    7, new SpellDefinition("Lich Form", 107, 0x5006, "Rel Xen Corp Ort", 25, 70, TargetType.Neutral, Reagents.DaemonBlood, Reagents.GraveDust, Reagents.NoxCrystal)
                },
                {
                    8, new SpellDefinition("Mind Rot", 108, 0x5007, "Wis An Ben", 17, 30, TargetType.Harmful, Reagents.BatWing, Reagents.DaemonBlood, Reagents.PigIron)
                },
                {
                    9, new SpellDefinition("Pain Spike", 109, 0x5008, "In Sar", 5, 20, TargetType.Harmful, Reagents.GraveDust, Reagents.PigIron)
                },
                {
                    10, new SpellDefinition("Poison Strike", 110, 0x5009, "In Vas Nox", 17, 50, TargetType.Harmful, Reagents.NoxCrystal)
                },
                {
                    11, new SpellDefinition("Strangle", 111, 0x500A, "In Bal Nox", 29, 65, TargetType.Harmful, Reagents.DaemonBlood, Reagents.NoxCrystal)
                },
                {
                    12, new SpellDefinition("Summon Familiar", 112, 0x500B, "Kal Xen Bal", 17, 30, TargetType.Neutral, Reagents.BatWing, Reagents.DaemonBlood, Reagents.GraveDust)
                },
                {
                    13, new SpellDefinition("Vampiric Embrace", 113, 0x500C, "Rel Xen An Sanct", 25, 99, TargetType.Neutral, Reagents.BatWing, Reagents.NoxCrystal, Reagents.PigIron)
                },
                {
                    14, new SpellDefinition("Vengeful Spirit", 114, 0x500D, "Kal Xen Bal Beh", 41, 80, TargetType.Harmful, Reagents.BatWing, Reagents.GraveDust, Reagents.PigIron)
                },
                {
                    15, new SpellDefinition("Wither", 115, 0x500E, "Kal Vas An Flam", 23, 60, TargetType.Harmful, Reagents.GraveDust, Reagents.NoxCrystal, Reagents.PigIron)
                },
                {
                    16, new SpellDefinition("Wraith Form", 116, 0x500F, "Rel Xen Um", 17, 20, TargetType.Neutral, Reagents.NoxCrystal, Reagents.PigIron)
                },
                {
                    17, new SpellDefinition("Exorcism", 117, 0x5010, "Ort Corp Grav", 40, 80, TargetType.Neutral, Reagents.NoxCrystal, Reagents.GraveDust)
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Necromancy.ToString();

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;

        public static SpellDefinition GetSpell(int spellIndex)
        {
            return _spellsDict.TryGetValue(spellIndex, out SpellDefinition spell) ? spell : SpellDefinition.EmptySpell;
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