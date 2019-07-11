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
    internal static class SpellsBushido
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;

        static SpellsBushido()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1, new SpellDefinition("Honorable Execution", 401, 0x5420, string.Empty, 0, 25, TargetType.Harmful, Reagents.None)
                },
                {
                    2, new SpellDefinition("Confidence", 402, 0x5421, string.Empty, 10, 25, TargetType.Beneficial, Reagents.None)
                },
                {
                    3, new SpellDefinition("Evasion", 403, 0x5422, string.Empty, 10, 60, TargetType.Beneficial, Reagents.None)
                },
                {
                    4, new SpellDefinition("Counter Attack", 404, 0x5423, string.Empty, 5, 40, TargetType.Harmful, Reagents.None)
                },
                {
                    5, new SpellDefinition("Lightning Strike", 405, 0x5424, string.Empty, 10, 50, TargetType.Harmful, Reagents.None)
                },
                {
                    6, new SpellDefinition("Momentum Strike", 406, 0x5425, string.Empty, 10, 70, TargetType.Harmful, Reagents.None)
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Bushido.ToString();

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