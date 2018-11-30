#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
    internal static class SpellsBushido
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;
        private static readonly List<SpellDefinition> _spells;

        static SpellsBushido()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1, new SpellDefinition("Honorable Execution", 401, 0x5420, string.Empty, 0, 25, Reagents.None)
                },
                {
                    2, new SpellDefinition("Confidence", 402, 0x5421, string.Empty, 10, 25, Reagents.None)
                },
                {
                    3, new SpellDefinition("Evasion", 403, 0x5422, string.Empty, 10, 60, Reagents.None)
                },
                {
                    4, new SpellDefinition("Counter Attack", 404, 0x5423, string.Empty, 5, 40, Reagents.None)
                },
                {
                    5, new SpellDefinition("Lightning Strike", 405, 0x5424, string.Empty, 10, 50, Reagents.None)
                },
                {
                    6, new SpellDefinition("Momentum Strike", 406, 0x5425, string.Empty, 10, 70, Reagents.None)
                }
            };
            _spells = _spellsDict.Values.ToList();
        }

        public static IReadOnlyList<SpellDefinition> Spells => _spells;

        public static SpellDefinition GetSpell(int spellIndex)
        {
            SpellDefinition spell;

            if (_spellsDict.TryGetValue(spellIndex, out spell))
                return spell;

            return SpellDefinition.EmptySpell;
        }
    }
}