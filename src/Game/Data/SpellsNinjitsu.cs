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
    internal static class SpellsNinjitsu
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        static SpellsNinjitsu()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1, new SpellDefinition("Focus Attack", 501, 0x5320, string.Empty, 20, 60, TargetType.Harmful, Reagents.None)
                },
                {
                    2, new SpellDefinition("Death Strike", 502, 0x5321, string.Empty, 30, 85, TargetType.Harmful, Reagents.None)
                },
                {
                    3, new SpellDefinition("Animal Form", 503, 0x5322, string.Empty, 0, 10, TargetType.Beneficial, Reagents.None)
                },
                {
                    4, new SpellDefinition("Ki Attack", 504, 0x5323, string.Empty, 25, 80, TargetType.Harmful, Reagents.None)
                },
                {
                    5, new SpellDefinition("Surprise Attack", 505, 0x5324, string.Empty, 20, 30, TargetType.Harmful, Reagents.None)
                },
                {
                    6, new SpellDefinition("Backstab", 506, 0x5325, string.Empty, 30, 20, TargetType.Harmful, Reagents.None)
                },
                {
                    7, new SpellDefinition("Shadowjump", 507, 0x5326, string.Empty, 15, 50, TargetType.Neutral, Reagents.None)
                },
                {
                    8, new SpellDefinition("Mirror Image", 508, 0x5327, string.Empty, 10, 40, TargetType.Neutral, Reagents.None)
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Ninjitsu.ToString();

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;

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