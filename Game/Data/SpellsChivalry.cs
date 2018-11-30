﻿#region license
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
    internal static class SpellsChivalry
    {
        private static readonly Dictionary<int, SpellDefinition> _spellsDict;
        private static readonly List<SpellDefinition> _spells;

        static SpellsChivalry()
        {
            _spellsDict = new Dictionary<int, SpellDefinition>
            {
                // Spell List
                {
                    1, new SpellDefinition("Cleanse by Fire", 201, 0x5100, "Expor Flamus", 10, 5, 10, Reagents.None)
                },
                {
                    2, new SpellDefinition("Close Wounds", 202, 0x5101, "Obsu Vulni", 10, 0, 10, Reagents.None)
                },
                {
                    3, new SpellDefinition("Consecrate Weapon", 203, 0x5102, "Consecrus Arma", 10, 15, 10, Reagents.None)
                },
                {
                    4, new SpellDefinition("Dispel Evil", 204, 0x5103, "Dispiro Malas", 10, 35, 10, Reagents.None)
                },
                {
                    5, new SpellDefinition("Divine Fury", 205, 0x5104, "Divinum Furis", 10, 25, 10, Reagents.None)
                },
                {
                    6, new SpellDefinition("Enemy of One", 206, 0x5105, "Forul Solum", 20, 45, 10, Reagents.None)
                },
                {
                    7, new SpellDefinition("Holy Light", 207, 0x5106, "Augus Luminos", 20, 55, 10, Reagents.None)
                },
                {
                    8, new SpellDefinition("Noble Sacrifice", 208, 0x5107, "Dium Prostra", 20, 65, 30, Reagents.None)
                },
                {
                    9, new SpellDefinition("Remove Curse", 209, 0x5108, "Extermo Vomica", 20, 5, 10, Reagents.None)
                },
                {
                    10, new SpellDefinition("Sacred Journey", 210, 0x5109, "Sanctum Viatas", 20, 5, 10, Reagents.None)
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