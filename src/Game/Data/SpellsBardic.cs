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

using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Data
{
    internal static class SpellsBardic
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        static SpellsBardic()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1, new SpellDefinition("Inspire", 701, 0x945, 0x945, "Uus Por", 30, 90, 8, TargetType.Beneficial, Reagents.None)
                },
                {
                    2, new SpellDefinition("Invigorate", 702, 0x946, 0x946, "An Zu", 30, 90, 8, TargetType.Beneficial, Reagents.None)
                },
                {
                    3, new SpellDefinition("Resilience", 703, 0x947, 0x947, "Kal Mani Tym", 30, 90, 8, TargetType.Beneficial, Reagents.None)
                },
                {
                    4, new SpellDefinition("Perseverance", 704, 0x948, 0x948, "Uus Jux Sanct", 30, 90, 8, TargetType.Beneficial, Reagents.None)
                },
                {
                    5, new SpellDefinition("Tribulation", 705, 0x949, 0x949, "In Jux Hur Rel", 30, 90, 16, TargetType.Harmful, Reagents.None)
                },
                {
                    6, new SpellDefinition("Despair", 706, 0x94A, 0x94A, "Kal Des Mani Tym", 30, 90, 18, TargetType.Harmful, Reagents.None)
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Bardic.ToString();

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;

        internal static string GetUsedSkillName(int spellid)
        {
            int div = (MaxSpellCount * 3) >> 3;

            if (div <= 0)
                div = 1;
            int group = spellid / div;

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