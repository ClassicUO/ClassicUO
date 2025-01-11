// SPDX-License-Identifier: BSD-2-Clause

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
                    1,
                    new SpellDefinition
                    (
                        "Honorable Execution",
                        401,
                        0x5420,
                        string.Empty,
                        0,
                        25,
                        TargetType.Harmful,
                        Reagents.None
                    )
                },
                {
                    2,
                    new SpellDefinition
                    (
                        "Confidence",
                        402,
                        0x5421,
                        string.Empty,
                        10,
                        25,
                        TargetType.Beneficial,
                        Reagents.None
                    )
                },
                {
                    3,
                    new SpellDefinition
                    (
                        "Evasion",
                        403,
                        0x5422,
                        string.Empty,
                        10,
                        60,
                        TargetType.Beneficial,
                        Reagents.None
                    )
                },
                {
                    4,
                    new SpellDefinition
                    (
                        "Counter Attack",
                        404,
                        0x5423,
                        string.Empty,
                        5,
                        40,
                        TargetType.Harmful,
                        Reagents.None
                    )
                },
                {
                    5,
                    new SpellDefinition
                    (
                        "Lightning Strike",
                        405,
                        0x5424,
                        string.Empty,
                        10,
                        50,
                        TargetType.Harmful,
                        Reagents.None
                    )
                },
                {
                    6,
                    new SpellDefinition
                    (
                        "Momentum Strike",
                        406,
                        0x5425,
                        string.Empty,
                        10,
                        70,
                        TargetType.Harmful,
                        Reagents.None
                    )
                }
            };
        }

        public static string SpellBookName { get; set; } = SpellBookType.Bushido.ToString();

        public static IReadOnlyDictionary<int, SpellDefinition> GetAllSpells => _spellsDict;
        internal static int MaxSpellCount => _spellsDict.Count;

        public static SpellDefinition GetSpell(int spellIndex)
        {
            if (_spellsDict.TryGetValue(spellIndex, out SpellDefinition spell))
            {
                return spell;
            }

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